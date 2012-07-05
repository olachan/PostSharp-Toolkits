using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

using PostSharp.Aspects;
using PostSharp.Aspects.Advices;
using PostSharp.Aspects.Configuration;
using PostSharp.Aspects.Internals;
using PostSharp.Aspects.Serialization;
using PostSharp.Extensibility;

namespace PostSharp.Toolkit.INPC
{
    internal class FieldDependenciesMap
    {
        /// <summary>
        /// Dictionary with a list of dependent properties for each instrumented field
        /// </summary>
        public static Dictionary<string, IList<string>> FieldDependentProperties { get; set; }
    }
}
