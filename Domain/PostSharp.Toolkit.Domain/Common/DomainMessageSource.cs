#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Domain.Common
{
    internal static class DomainMessageSource
    {
        public static MessageSource Instance = new MessageSource( "PostSharp.Toolkit.DOM", new DomainMessageDispenser() );

        private class DomainMessageDispenser : MessageDispenser
        {
            public DomainMessageDispenser()
                : base( "DOM" )
            {
            }

            protected override string GetMessage( int number )
            {
                switch ( number )
                {
                    case 1:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " +
                               "Method {1} contains direct access to a field of another class." +
                               "Use NotifyPropertyChangeSafe attribute to specify that property value depends only on state of current instance or DependsOn attribute to explicitly specify dependencies.";
                    case 2:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " +
                               "Method {1} contains call to non void (ref/out param) method of another class." +
                               "Use NotifyPropertyChangeSafe attribute to specify that property value depends only on state of current instance, DependsOn attribute to explicitly specify dependencies or mark called method with IdempotentMethodAttribute attribute to specify that the method is idempotent.";
                    case 3:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " + "Method {1} contains delegate call." +
                               "Use NotifyPropertyChangeSafe attribute to specify that property value depends only on state of current instance or DependsOn attribute to explicitly specify dependencies.";
                    case 4:
                        return
                            "Method {0} contains parameters of not intrinsic type. IdempotentMethodAttribute can be applied only to methods with intrinsic(primitive) parameters";
                    case 5:
                        return "Type {0} implements INotifyPropertyChanged without implementing INotifyChildPropertyChanged";
                    case 6:
                        return
                            "Base class of type {0} implements INotifyChildPropertyChanged or INotifyPropertyChanged but is not instrumented with NotifyPropertyChangedAttribute";
                    case 7:
                        return "Property {0} depends no more than 5 fields of matching type. Some optimizations not possible";
                    case 8:
                        return
                            "Class {0} implements INotifyPropertyChanged but does not define an OnPropertyChanged method with the following signature: void OnPropertyChanged(string propertyName).";
                    case 9:
                        return
                            "Class {0} defines event PostSharpToolkitsDomain_ChildPropertyChanged or method PostSharpToolkitsDomain_OnChildPropertyChanged with the following signature: void PostSharpToolkitsDomain_OnChildPropertyChanged(string propertyPath).";
                    case 10:
                        return
                            "Class {0} defines IntroduceNotifyPropertyChangedAttribute or IntroduceNotifyChildPropertyChangedAttribute which can be applied only once in object hierarchy";
                    case 11:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. " + "Method {1} contains virtual method call." +
                               "Use InstanceScopedProperty attribute to specify that property value depends only on state of current instance or DependsOn attribute to explicitly specify dependencies.";
                    case 12:
                        return "Depends.On method used within {0} which is not a property";
                    case 13:
                        return "ImplicitOperationManagementAttribute: automatic analysis of property {0} failed. Place ChangeTrackedAttribute on field or property containing tracked object.";
                    
                    case 14:
                        return "NotifyPropertyChangedAttribute: automatic analysis of property {0} failed. Field {1} has open generic type.";
                    case 15:
                        return "ImplicitOperationManagementAttribute: automatic analysis of property {0} failed. Place ChangeTrackingIgnoreField on field or property using one field.";
                    case 16:
                        return "EditableObjectAttribute: type {0} requested implementation of IEditablaObject with EditableObjectAttribute but contains methods implementing IEditableObject that contain other declarations than throwing ToBeImplementedException";
                    default:
                        return null;
                }
            }
        }
    }
}