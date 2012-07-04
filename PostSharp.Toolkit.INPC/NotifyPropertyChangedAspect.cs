using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
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
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict)]
    [IntroduceInterface(typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    [IntroduceInterface(typeof(IRaiseNotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    public class NotifyPropertyChangedAspect : InstanceLevelAspect, INotifyPropertyChanged, IRaiseNotifyPropertyChanged
    {
        // runtime immiutable hance thread synchronization is not needed
        
        private Dictionary<string, IList<string>> fieldPropertyMapping;

        //[NonSerialized]
        //private ThreadLocal<int> methodCounter;

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            this.fieldPropertyMapping = FieldMap.FieldPropertyMapping;
            FieldMap.FieldPropertyMapping = null;
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            if (FieldMap.FieldPropertyMapping == null && this.fieldPropertyMapping != null)
            {
                FieldMap.FieldPropertyMapping = this.fieldPropertyMapping;
            }
        }

        [NonSerialized]
        [ThreadStatic]
        private static Stack<object> stackTrace;

        //[NonSerialized]
        //private ThreadLocal<bool> isInpcFireing; 

        private static Stack<object> StackTrace
        {
            get
            {
                return stackTrace ?? (stackTrace = new Stack<object>());
            }
        }

        // used compile time only
        [NonSerialized]
        private static HashSet<MethodBase> analizedMethods;

        // used compile time only
        [NonSerialized]
        private static Dictionary<MethodBase, IList<FieldInfo>> methodFieldMapping;

        [OnLocationSetValueAdvice, MulticastPointcut(Targets = MulticastTargets.Field)]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            args.ProceedSetValue();
            IList<string> propertyList;
            if (FieldMap.FieldPropertyMapping.TryGetValue(args.LocationFullName, out propertyList))
            {
                foreach (string propertyName in propertyList)
                {
                    ChangedPropertyAcumulator.AddProperty(args.Instance, propertyName);
                }
            }

            RaisePropertyChanged(args.Instance, 0);

        }

        private void RaisePropertyChanged(object instance, int maxStackCount)
        {
            //if (this.isInpcFireing.Value)
            //{
            //    return;
            //}
            if (StackTrace.Count(o => o == instance) > maxStackCount)
            {
                return;
            }


            try
            {
                //this.isInpcFireing.Value = true;

                var objectsToRisePropertyChanged = ChangedPropertyAcumulator.ChangedObjects.Except(StackTrace).Union(new[] { instance });

                foreach (object o in objectsToRisePropertyChanged)
                {
                    IList<string> propertyNames;
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
                            rpc.OnPropertyChanged(changedProperty);
                        }
                    }

                    ChangedPropertyAcumulator.ChangedProperties.Remove(o);
                }
            }
            finally
            {
                //this.isInpcFireing.Value = false;
            }
        }

        [OnMethodInvokeAdvice, MulticastPointcut(Targets = MulticastTargets.Method)]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            if (args.Method.GetCustomAttributes(typeof(NotNotifiedAttribute), true).Any())
            {
                args.Proceed();
                return;
            }

            try
            {
                //if (methodCounter.Value == 0)
                //{
                StackTrace.Push(args.Instance);
                //}

                //methodCounter.Value++;
                args.Proceed();
            }
            finally
            {
                //methodCounter.Value--;
                //if (methodCounter.Value == 0)
                {
                    this.RaisePropertyChanged(args.Instance, 1);
                    StackTrace.Pop(); //TODO: chack if pop is always valid
                }
            }
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            // this.fieldMap = (FieldMap)type.Assembly.GetCustomAttributes( typeof(FieldMap), false ).First();

            if (FieldMap.FieldPropertyMapping == null) FieldMap.FieldPropertyMapping = new Dictionary<string, IList<string>>();

            methodFieldMapping = new Dictionary<MethodBase, IList<FieldInfo>>();
            analizedMethods = new HashSet<MethodBase>();

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
                        IList<string> propertyList = FieldMap.FieldPropertyMapping.GetOrCreate(string.Format("{0}.{1}", field.DeclaringType.FullName, field.Name), () => new List<string>());

                        propertyList.AddIfNew(propertyInfo.Name);
                    }

                }
            }
        }

        public override void RuntimeInitialize(Type type)
        {
           
            //this.methodCounter = new ThreadLocal<int>(() => 0);
        }

        //public override object CreateInstance(AdviceArgs adviceArgs)
        //{
        //    NotifyPropertyChangedAspect instance = (NotifyPropertyChangedAspect)this.MemberwiseClone();
        //    instance.methodCounter = new ThreadLocal<int>();
        //    instance.isInpcFireing = new ThreadLocal<bool>();
        //    return instance;
        //}

        private void AnalizeMethod(Type type, MethodBase method)
        {
            if (analizedMethods.Contains(method))
            {
                return;
            }

            MethodUsageCodeReference[] declarations = ReflectionSearch.GetDeclarationsUsedByMethod(method);

            IList<FieldInfo> fieldList = methodFieldMapping.GetOrCreate(method, () => new List<FieldInfo>());

            foreach (var reference in declarations.Where(r => r.UsedType.IsAssignableFrom( type )))
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
            //methodCounter = new ThreadLocal<int>(() => 0);
            //isInpcFireing = new ThreadLocal<bool>(() => false);
        }

        [ImportMember("OnPropertyChanged", IsRequired = false, Order = ImportMemberOrder.AfterIntroductions)]
        public Action<string> OnPropertyChangedMethod;

        [IntroduceMember(Visibility = Visibility.Family, IsVirtual = true, OverrideAction = MemberOverrideAction.Ignore)]
        public void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this.Instance, new PropertyChangedEventArgs(propertyName));
            }
        }

        [IntroduceMember(OverrideAction = MemberOverrideAction.Ignore)]
        public event PropertyChangedEventHandler PropertyChanged;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class NotNotifiedAttribute : Attribute
    {
    }

    public interface IRaiseNotifyPropertyChanged
    {
        void OnPropertyChanged(string propertyName);
    }
}
