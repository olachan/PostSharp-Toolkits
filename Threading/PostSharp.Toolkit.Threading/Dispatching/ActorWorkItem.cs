using System;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading.Dispatching
{
    internal class ActorWorkItem : WorkItem
    {
        public ActorWorkItem( MethodInterceptionArgs args, bool clone ) : base( args, clone )
        {
        }

        protected override void OnException(Exception e, ref bool handled)
        {
            ((Actor) this.Instance).CallOnException( e, ref handled );
        }
    }
}