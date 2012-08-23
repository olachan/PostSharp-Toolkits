using System;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Domain.OperationTracking
{
    [Serializable]
    [IntroduceInterface( typeof(ITrackedObject), OverrideAction = InterfaceOverrideAction.Ignore, AncestorOverrideAction = InterfaceOverrideAction.Ignore )]
    [MulticastAttributeUsage( MulticastTargets.Class, Inheritance = MulticastInheritance.Strict )]
    internal class ChunkManagementAttribute : TrackedObjectAttributeBase
    {
        [OnMethodInvokeAdvice]
        [MethodPointcut("SelectMethods")]
        public override void OnMethodInvoke(MethodInterceptionArgs args)
        {
            base.OnMethodInvoke(args);
        }
    }
}