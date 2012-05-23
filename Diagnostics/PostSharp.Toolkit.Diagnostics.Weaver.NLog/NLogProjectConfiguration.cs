#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Configuration;
using PostSharp.Toolkit.Diagnostics.Weaver.NLog;
using PostSharp.Toolkit.Diagnostics.Weaver.NLog.Logging;

[assembly: AssemblyProjectProvider( typeof(NLogProjectConfiguration) )]

namespace PostSharp.Toolkit.Diagnostics.Weaver.NLog
{
    public class NLogProjectConfiguration : IProjectConfigurationProvider
    {
        public ProjectConfiguration GetProjectConfiguration()
        {
            ProjectConfiguration projectConfiguration = new ProjectConfiguration
                                                            {
                                                                Services = new ServiceConfigurationCollection
                                                                               {
                                                                                   new ServiceConfiguration( project => new NLogBackendProvider() )
                                                                               },
                                                            };

            return projectConfiguration;
        }
    }
}