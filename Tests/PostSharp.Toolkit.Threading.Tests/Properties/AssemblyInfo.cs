#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Reflection;
using System.Runtime.InteropServices;
using PostSharp.Toolkit.Threading.DeadlockDetection;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.

[assembly: AssemblyTitle( "PostSharp.Toolkit.Threading.Tests" )]
[assembly: AssemblyDescription( "" )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "" )]
[assembly: AssemblyProduct( "PostSharp.Toolkit.Threading.Tests" )]
[assembly: AssemblyCopyright( "Copyright ©  2012" )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.

[assembly: ComVisible( false )]

// The following GUID is for the ID of the typelib if this project is exposed to COM

[assembly: Guid( "830fe37a-edb4-47ea-95f1-a3344b8e59e2" )]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]

[assembly: AssemblyVersion( "1.0.0.0" )]
[assembly: AssemblyFileVersion( "1.0.0.0" )]
[assembly: DeadlockDetectionPolicy( AttributeTargetAssemblies = "mscorlib", AttributeTargetTypes = "System.Threading.*" )]
[assembly: DeadlockDetectionPolicy( AttributeTargetAssemblies = "System.Core", AttributeTargetTypes = "System.Threading.*" )]