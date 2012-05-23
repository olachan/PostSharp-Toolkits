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

namespace PostSharp.Toolkit.Threading.Synchronization
{
    /// <summary>
    /// Custom attribute that, when applied on a class, automatically implements
    /// the <see cref="IReaderWriterSynchronized"/> interface. A new <see cref="ReaderWriterLockSlim"/>
    /// object is created upon each instantiation of the target class.
    /// </summary>
    [Serializable]
    [CompositionAspectConfiguration( SerializerType = typeof(MsilAspectSerializer) )]
    [IntroduceInterface( typeof(IReaderWriterSynchronized), OverrideAction = InterfaceOverrideAction.Ignore )]
    [ProvideAspectRole( StandardRoles.Threading )]
    [MulticastAttributeUsage( MulticastTargets.Class, Inheritance = MulticastInheritance.Strict )]
    public sealed class ReaderWriterSynchronizedAttribute : InstanceLevelAspect, IReaderWriterSynchronized
    {
        [NonSerialized] private ReaderWriterLockSlim @lock;

        [ThreadStatic] private static HashSet<object> pendingConstructors;


        public override object CreateInstance( AdviceArgs aspectArgs )
        {
            ReaderWriterSynchronizedAttribute instance = new ReaderWriterSynchronizedAttribute();
            return instance;
        }

        public ReaderWriterLockSlim Lock
        {
            get { return LazyInitializer.EnsureInitialized( ref this.@lock, () => new ReaderWriterLockSlim( LockRecursionPolicy.NoRecursion ) ); }
        }


        public bool CheckFieldAccess { get; set; }

        [OnMethodEntryAdvice, MethodPointcut( "SelectConstructors" )]
        public void OnEnterConstructor( MethodExecutionArgs args )
        {
            HashSet<object> myPendingConstructors = pendingConstructors;
            if ( pendingConstructors == null ) pendingConstructors = myPendingConstructors = new HashSet<object>();
            myPendingConstructors.Add( args.Instance );
        }

        [OnMethodExitAdvice( Master = "OnEnterConstructor" )]
        public void OnExitConstructor( MethodExecutionArgs args )
        {
            pendingConstructors.Remove( args.Instance );
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

        [OnLocationSetValueAdvice, MethodPointcut( "SelectFields" )]
        public void OnFieldSet( LocationInterceptionArgs args )
        {
            if ( pendingConstructors != null && pendingConstructors.Contains( args.Instance ) ) return;

            if ( !((IReaderWriterSynchronized) args.Instance).Lock.IsWriteLockHeld )
                throw new LockNotHeldException( string.Format( "A writer lock is necessary to access field '{0}'.", args.Location.Name ) );
        }
    }
}