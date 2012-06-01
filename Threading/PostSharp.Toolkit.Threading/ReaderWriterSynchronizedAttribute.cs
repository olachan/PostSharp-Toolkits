#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Dependencies;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Threading
{
    /// <summary>
    /// Custom attribute that, when applied on a class, automatically implements
    /// the <see cref="IReaderWriterSynchronized"/> interface. A new <see cref="ReaderWriterLockSlim"/>
    /// object is created upon each instantiation of the target class.
    /// </summary>
    [Serializable]
    [CompositionAspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    [IntroduceInterface( typeof(IReaderWriterSynchronized), OverrideAction = InterfaceOverrideAction.Ignore )]
    [MulticastAttributeUsage( MulticastTargets.Class, Inheritance = MulticastInheritance.Strict )]
    // [AspectRoleDependency(AspectDependencyAction.Conflict, ThreadingToolkitAspectRoles.ThreadingModel)]
    [ProvideAspectRole(ThreadingToolkitAspectRoles.ThreadingModel)]
    public sealed class ReaderWriterSynchronizedAttribute : InstanceLevelAspect, IReaderWriterSynchronized
    {
        [NonSerialized] private ReaderWriterLockSlim @lock;

        [ThreadStatic] private static HashSet<object> runningConstructors;
        [ThreadStatic] private static Dictionary<ReaderWriterLockSlim, ReadCheckNode> runningMethods;


        public override bool CompileTimeValidate(Type type)
        {
            bool result = base.CompileTimeValidate(type);

            // All fields should be private or protected unless marked as [ThreadSafe]. [Error]
            foreach (var publicField in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static).Where(f => f.GetCustomAttributes(typeof(ThreadSafeAttribute), false).Length == 0))
            {
                ThreadingMessageSource.Instance.Write(type, SeverityType.Error, "THR007", type.Name, publicField.Name);
                result = false;
            }

            // TODO: Fields cannot be accessed from a static method unless the method or the field is marked as [ThreadSafe]. [Warning]

            return result;
        }

        public override object CreateInstance( AdviceArgs aspectArgs )
        {
            ReaderWriterSynchronizedAttribute instance = new ReaderWriterSynchronizedAttribute();
            return instance;
        }

        public ReaderWriterLockSlim Lock
        {
            get { return LazyInitializer.EnsureInitialized( ref this.@lock, () => new ReaderWriterLockSlim( LockRecursionPolicy.SupportsRecursion ) ); }
        }


        public bool CheckFieldAccess { get; set; }

        [OnMethodEntryAdvice, MethodPointcut( "SelectConstructors" )]
        public void OnEnterConstructor( MethodExecutionArgs args )
        {
            HashSet<object> myPendingConstructors = runningConstructors;
            if ( runningConstructors == null ) runningConstructors = myPendingConstructors = new HashSet<object>();
            myPendingConstructors.Add( args.Instance );
        }

        [OnMethodExitAdvice( Master = "OnEnterConstructor" )]
        public void OnExitConstructor( MethodExecutionArgs args )
        {
            runningConstructors.Remove( args.Instance );
        }

        [OnLocationSetValueAdvice, MethodPointcut("SelectFields")]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            if (runningConstructors != null && runningConstructors.Contains(args.Instance)) return;

            if (!((IReaderWriterSynchronized)args.Instance).Lock.IsWriteLockHeld)
                throw new LockNotHeldException(string.Format("A writer lock is necessary to access field '{0}'.", args.Location.Name));
        }

        [OnLocationGetValueAdvice(Master = "OnFieldSet")]
        public void OnFieldGet(LocationInterceptionArgs args)
        {
            if (runningConstructors != null && runningConstructors.Contains(args.Instance)) return;

            ReaderWriterLockSlim myLock = ((IReaderWriterSynchronized)args.Instance).Lock;
            if (myLock.IsReadLockHeld) return;

            Dictionary<ReaderWriterLockSlim, ReadCheckNode> myRunningMethods = runningMethods;
            ReadCheckNode node;
            if (myRunningMethods == null || !myRunningMethods.TryGetValue( myLock, out node ))
            {
                // The field is read from outside an instance method of the current instance.
                // This can happen. This would not be a good practice, but this should be checked using an architectural constraint.

                // TODO: Verify with architectural constraint.
                return;
            }

            if ( node.Count > 0 )
                throw new LockNotHeldException("A reader lock is necessary to access more than one field.");

            node.Count = 1;
        }

        [OnMethodEntryAdvice, MethodPointcut("SelectMethods")]
        public void OnEnterMethod( MethodExecutionArgs args )
        {
            ReaderWriterLockSlim myLock = ((IReaderWriterSynchronized)args.Instance).Lock;

            Dictionary<ReaderWriterLockSlim, ReadCheckNode> myRunningMethods = runningMethods ??
                                                                               (runningMethods = new Dictionary<ReaderWriterLockSlim, ReadCheckNode>());
            if ( !myRunningMethods.ContainsKey( myLock ))
            {
                myRunningMethods.Add( myLock, new ReadCheckNode() );
                args.MethodExecutionTag = myLock;
            }

        }

        [OnMethodExitAdvice(Master = "OnEnterMethod")]
        public void OnExitMethod(MethodExecutionArgs args)
        {
            if ( args.MethodExecutionTag != null )
            {
                runningMethods.Remove((ReaderWriterLockSlim) args.MethodExecutionTag);
            }
        }

        public IEnumerable<MethodBase> SelectConstructors( Type type )
        {
            if ( this.CheckFieldAccess )
            {
                return type.GetConstructors( BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic );
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<FieldInfo> SelectFields( Type type )
        {
            if ( this.CheckFieldAccess )
            {
                return
                    type.GetFields( BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic ).Where(
                        f => f.GetCustomAttributes( typeof(ThreadSafeAttribute), false ).Length == 0 );
            }
            else
            {
                return null;
            }
        }

        public IEnumerable<MethodInfo> SelectMethods(Type type)
        {
            if (this.CheckFieldAccess)
            {
                return
                    type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Where(
                        f => f.GetCustomAttributes(typeof(ThreadSafeAttribute), false).Length == 0);
            }
            else
            {
                return null;
            }
        }

       
        class ReadCheckNode
        {
            public int Count;
        }
    }
}