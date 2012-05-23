#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Aspects;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.Diagnostics
{
    [RequirePostSharp( "PostSharp.Toolkit.Diagnostics.Weaver", "PostSharp.Toolkit.Diagnostics" )]
    public interface ILogAspect : IAspect
    {
    }
}