#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using PostSharp.Aspects;

namespace PostSharp.Toolkit.Threading
{
    internal class ActorWorkItem : WorkItem
    {
        public ActorWorkItem( MethodInterceptionArgs args, bool clone ) : base( args, clone )
        {
        }

        protected override void OnException( Exception e, ref bool handled )
        {
            ((Actor) this.Instance).CallOnException( e, ref handled );
        }
    }
}