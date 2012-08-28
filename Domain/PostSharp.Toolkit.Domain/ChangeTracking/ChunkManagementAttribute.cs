using System;
using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Domain.ChangeTracking
{
    [Serializable]
    [IntroduceInterface( typeof(ITrackedObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore )]
    [MulticastAttributeUsage( MulticastTargets.Class, Inheritance = MulticastInheritance.Strict )]
    internal class ChunkManagementAttribute : TrackedObjectAttributeBase
    {
        [OnMethodInvokeAdvice]
        [MethodPointcut("SelectMethods")]
        public void OnMethodInvoke(MethodInterceptionArgs args)
        {
            base.OnMethodInvokeBase(args);
        }
    }
}