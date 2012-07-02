using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Extensibility;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.INPC
{
    /// <summary>
    /// Under development. Early version !!!
    /// </summary>
    [Serializable]
    [IntroduceInterface(typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    [IntroduceInterface(typeof(IRaiseNotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    public class NotifyPropertyChangedAspect : InstanceLevelAspect, INotifyPropertyChanged, IRaiseNotifyPropertyChanged
    {
        private Dictionary<string, IList<string>> fieldPropertyMapping;

        [NonSerialized]
        private ThreadLocal<int> methodCounter;

        [NonSerialized]
        [ThreadStatic]
        private static Stack<object> stackTrace;

        private static Stack<object> StackTrace
        {
            get
            {
                return stackTrace ?? (stackTrace = new Stack<object>());
            }
        }

        [NonSerialized]
        private static HashSet<MethodBase> analizedMethods = new HashSet<MethodBase>();

        [NonSerialized]
        private static Dictionary<MethodBase, IList<FieldInfo>> methodFieldMapping;

        [OnLocationSetValueAdvice, MulticastPointcut(Targets = MulticastTargets.Field)]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            args.ProceedSetValue();
            IList<string> propertyList;
            if (fieldPropertyMapping.TryGetValue(args.LocationName, out propertyList))
            {
                foreach (string propertyName in propertyList)
                {
                    ChangedPropertyAcumulator.AddProperty(this.Instance, propertyName);
                }
            }

            if (methodCounter.Value == 0)
            {
                RaisePropertyChanged();
            }
        }

        private void RaisePropertyChanged()
        {
            IList<string> propertyNames;

            var objectsToRisePropertyChanged = ChangedPropertyAcumulator.ChangedObjects.Except( StackTrace ).Union( new[] { this.Instance } );

            foreach ( object o in objectsToRisePropertyChanged )
            {
                ChangedPropertyAcumulator.ChangedProperties.TryGetValue(o, out propertyNames);

                if (propertyNames == null)
                {
                    continue;
                }

                foreach (var changedProperty in propertyNames)
                {
                    IRaiseNotifyPropertyChanged rpc = o as IRaiseNotifyPropertyChanged;
                    if (rpc != null)
                    {
                        rpc.OnPropertyChanged( changedProperty );
                    }
                }

                ChangedPropertyAcumulator.ChangedProperties.Remove(o);
            }
        }

        [OnMethodInvokeAdvice, MulticastPointcut(Targets = MulticastTargets.Method)]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            try
            {
                if (methodCounter.Value == 0)
                {
                    StackTrace.Push( this.Instance );
                }

                methodCounter.Value++;
                args.Proceed();
            }
            finally
            {
                methodCounter.Value--;
                if (methodCounter.Value == 0)
                {
                    this.RaisePropertyChanged();
                    StackTrace.Pop(); //TODO: chack if pop is always valid
                }
            }
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            fieldPropertyMapping = new Dictionary<string, IList<string>>();
            methodFieldMapping = new Dictionary<MethodBase, IList<FieldInfo>>();

            base.CompileTimeInitialize(type, aspectInfo);

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(p => !p.GetCustomAttributes(typeof(NotNotifiedAttribute), true).Any());

            foreach (var propertyInfo in properties)
            {
                var getMethod = propertyInfo.GetGetMethod(false);

                if (getMethod == null)
                {
                    continue;
                }

                this.AnalizeMethod(type, getMethod);

                IList<FieldInfo> fieldList;

                if (methodFieldMapping.TryGetValue(getMethod, out fieldList))
                {
                    foreach (var field in fieldList)
                    {
                        IList<string>  propertyList = fieldPropertyMapping.GetOrCreate(field.Name, () => new List<string>());

                        propertyList.AddIfNew(propertyInfo.Name);
                    }

                }
            }
        }

        private void AnalizeMethod(Type type, MethodBase method)
        {
            if (analizedMethods.Contains(method))
            {
                return;
            }

            MethodUsageCodeReference[] declarations = ReflectionSearch.GetDeclarationsUsedByMethod(method);

            IList<FieldInfo> fieldList = methodFieldMapping.GetOrCreate( method, () => new List<FieldInfo>() );

            foreach (var reference in declarations.Where(r => r.UsedType == type))
            {
                if (reference.Instructions.HasFlag(MethodUsageInstructions.LoadField))
                {
                    fieldList.AddIfNew((FieldInfo)reference.UsedDeclaration);
                }

                if (reference.Instructions.HasFlag(MethodUsageInstructions.Call))
                {
                    MethodBase calledMethod = (MethodBase)reference.UsedDeclaration;
                    this.AnalizeMethod(type, calledMethod);
                    IList<FieldInfo> calledMethodFields;
                    methodFieldMapping.TryGetValue(calledMethod, out calledMethodFields);

                    if (calledMethodFields != null)
                    {
                        foreach (var calledMethodField in calledMethodFields)
                        {
                            fieldList.AddIfNew(calledMethodField);
                        }
                    }
                }
            }

        }

        public override void RuntimeInitializeInstance()
        {
            base.RuntimeInitializeInstance();
            methodCounter = new ThreadLocal<int>(() => 0);
        }

        [ImportMember("OnPropertyChanged", IsRequired = false, Order = ImportMemberOrder.AfterIntroductions)]
        public Action<string> OnPropertyChangedMethod;

        [IntroduceMember(Visibility = Visibility.Family, IsVirtual = true, OverrideAction = MemberOverrideAction.Ignore)]
        public void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this.Instance, new PropertyChangedEventArgs(propertyName));
            }
        }

        [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore)]
        public event PropertyChangedEventHandler PropertyChanged;
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class NotNotifiedAttribute : Attribute
    {
    }

    public interface IRaiseNotifyPropertyChanged
    {
        void OnPropertyChanged( string propertyName );
    }

}
