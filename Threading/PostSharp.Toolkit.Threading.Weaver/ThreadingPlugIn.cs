using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.AspectWeaver;

namespace PostSharp.Toolkit.Threading.Weaver
{
    internal class ThreadingPlugIn : AspectWeaverPlugIn
    {
        public const string Name = "PostSharp.Toolkit.Threading";

        public ThreadingPlugIn()
            : base( StandardPriorities.User )
        {
        }
        protected override void Initialize()
        {
            this.BindAspectWeaver<DispatchedMethodAttribute.AsyncStateMachineAspect, AsyncStateMachineAspectWeaver>();
        }
    }
}
