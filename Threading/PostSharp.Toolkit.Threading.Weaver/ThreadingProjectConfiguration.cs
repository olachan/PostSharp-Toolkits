#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System.Collections.Generic;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Configuration;
using PostSharp.Toolkit.Threading.Weaver;

[assembly: AssemblyProjectProvider(typeof(ThreadingProjectConfiguration))]

namespace PostSharp.Toolkit.Threading.Weaver
{
    public sealed class ThreadingProjectConfiguration : IProjectConfigurationProvider
    {
        public ProjectConfiguration GetProjectConfiguration()
        {
            ProjectConfiguration projectConfiguration = new ProjectConfiguration
                                                            {
                                                                TaskTypes = new TaskTypeConfigurationCollection
                                                                                {
                                                                                    new TaskTypeConfiguration( ThreadingPlugIn.Name,
                                                                                                               project => new ThreadingPlugIn() )
                                                                                        {
                                                                                            AutoInclude = true,
                                                                                            Dependencies = new DependencyConfigurationCollection
                                                                                                               {
                                                                                                                   new DependencyConfiguration( "AspectWeaver" )
                                                                                                               }
                                                                                        }
                                                                                },
                                                                TaskFactories = new Dictionary<string, CreateTaskDelegate>
                                                                                    {
                                                                                        {ThreadingPlugIn.Name, project => new ThreadingPlugIn()}
                                                                                    }
                                                            };


            return projectConfiguration;
        }
    }
}