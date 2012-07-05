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
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Toolkit.INPC
{
    /// <summary>
    /// Under development. Early version !!!
    /// </summary>
    [Serializable]
    [MulticastAttributeUsage(MulticastTargets.Class, Inheritance = MulticastInheritance.Strict)]
    [IntroduceInterface(typeof(INotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    [IntroduceInterface(typeof(IRaiseNotifyPropertyChanged), OverrideAction = InterfaceOverrideAction.Ignore)]
    public class NotifyPropertyChangedAttribute : InstanceLevelAspect, IRaiseNotifyPropertyChanged
    {
        // Compile-time use only
        private static PropertiesDependencieAnalyzer analyzer = new PropertiesDependencieAnalyzer();

        //TODO: Encapsulate the map into a class instead of using this ugly dictionary everywhere
        private Dictionary<string, IList<string>> fieldDependentProperties;

        [OnSerializing]
        public void OnSerializing(StreamingContext context)
        {
            //TODO: Consider better serialization mechanism - maybe simply introduce assembly level aspect doing the stuff below?

            //Grab the dependencies map to serialize if, if no other aspect has done it before
            if (analyzer != null)
            {
                this.fieldDependentProperties = analyzer.FieldDependentProperties;
                analyzer = null;
            }
        }

        [OnDeserialized]
        public void OnDeserialized(StreamingContext context)
        {
            //If dependencies map was serialized within this aspect, copy the data to global map
            if (FieldDependenciesMap.FieldDependentProperties == null && this.fieldDependentProperties != null)
            {
                // TODO: should add to map not replace it
                FieldDependenciesMap.FieldDependentProperties = this.fieldDependentProperties;
            }
        }

        [OnLocationSetValueAdvice, MulticastPointcut(Targets = MulticastTargets.Field)]
        public void OnFieldSet(LocationInterceptionArgs args)
        {
            args.ProceedSetValue();
            IList<string> propertyList;
            if (FieldDependenciesMap.FieldDependentProperties.TryGetValue(args.LocationFullName, out propertyList))
            {
                foreach (string propertyName in propertyList)
                {
                    PropertyChangesTracker.Accumulator.AddProperty(args.Instance, propertyName);
                }
            }

            PropertyChangesTracker.RaisePropertyChanged(args.Instance, false);
        }

        [OnMethodInvokeAdvice, MulticastPointcut(Targets = MulticastTargets.Method)]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            if (args.Method.GetCustomAttributes(typeof(NoAutomaticPropertyChangedNotificationsAttribute), true).Any())
            {
                args.Proceed();
                return;
            }

            try
            {
                PropertyChangesTracker.StackContext.PushOnStack(args.Instance);
                args.Proceed();
            }
            finally
            {
                {
                    PropertyChangesTracker.RaisePropertyChanged(args.Instance, true);
                }
            }
        }

        public override void CompileTimeInitialize(Type type, AspectInfo aspectInfo)
        {
            analyzer.AnalyzeType( type );
        }

        

        //[ImportMember("OnPropertyChanged", IsRequired = false, Order = ImportMemberOrder.AfterIntroductions)]
        //public Action<string> OnPropertyChangedMethod;

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

    //TODO: Rename!
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class NoAutomaticPropertyChangedNotificationsAttribute : Attribute
    {
    }
}
