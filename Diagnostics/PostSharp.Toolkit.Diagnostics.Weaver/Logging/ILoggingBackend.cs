#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Sdk.AspectWeaver;
using PostSharp.Sdk.CodeModel;

namespace PostSharp.Toolkit.Diagnostics.Weaver.Logging
{
    public interface ILoggingBackend
    {
        void Initialize( ModuleDeclaration module );
        ILoggingBackendInstance CreateInstance( AspectWeaverInstance aspectWeaverInstance );
    }
}