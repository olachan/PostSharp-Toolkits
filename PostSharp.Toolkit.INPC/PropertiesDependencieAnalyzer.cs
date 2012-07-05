#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PostSharp.Reflection;

namespace PostSharp.Toolkit.INPC
{
    public class PropertiesDependencieAnalyzer
    {
        //Methods already analyzed (for redundant analysis and cycles avoidance)
        private readonly HashSet<MethodBase> analyzedMethods = new HashSet<MethodBase>();

        //Dependencies of methods on fields
        private static readonly Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies = new Dictionary<MethodBase, IList<FieldInfo>>();

        private readonly Dictionary<string, IList<string>> fieldDependentProperties = new Dictionary<string, IList<string>>();

        public Dictionary<string, IList<string>> FieldDependentProperties
        {
            get { return this.fieldDependentProperties; }
        }


        public void AnalyzeType(Type type)
        {
            //We need to grab all the properties and build a map of their dependencies

            if (FieldDependenciesMap.FieldDependentProperties == null) FieldDependenciesMap.FieldDependentProperties = new Dictionary<string, IList<string>>();

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.GetCustomAttributes(typeof(NoAutomaticPropertyChangedNotificationsAttribute), true).Any());

            foreach (var propertyInfo in properties)
            {
                var getMethod = propertyInfo.GetGetMethod(false);

                if (getMethod == null)
                {
                    continue;
                }

                this.AnalyzeMethod(type, getMethod);

                IList<FieldInfo> fieldList;

                //Time to build the reversed graph, i.e. field->property dependencies

                if (methodFieldDependencies.TryGetValue(getMethod, out fieldList))
                {
                    foreach (var field in fieldList)
                    {
                        IList<string> propertyList = fieldDependentProperties.GetOrCreate(string.Format("{0}.{1}", field.DeclaringType.FullName, field.Name), () => new List<string>());

                        propertyList.AddIfNew(propertyInfo.Name);
                    }

                }
            }
        }


        private void AnalyzeMethod(Type type, MethodBase method)
        {
            //TODO: Rewrite using AST to find out whether we're not accessing other instances, report errors etc.

            if (analyzedMethods.Contains(method))
            {
                return;
            }

            analyzedMethods.Add( method );

            MethodUsageCodeReference[] declarations = ReflectionSearch.GetDeclarationsUsedByMethod(method);

            IList<FieldInfo> fieldList = methodFieldDependencies.GetOrCreate(method, () => new List<FieldInfo>());

            foreach (var reference in declarations.Where(r => r.UsedType.IsAssignableFrom(type)))
            {
                if (reference.Instructions.HasFlag(MethodUsageInstructions.LoadField))
                {
                    fieldList.AddIfNew((FieldInfo)reference.UsedDeclaration);
                }

                if (reference.Instructions.HasFlag(MethodUsageInstructions.Call))
                {
                    MethodBase calledMethod = (MethodBase)reference.UsedDeclaration;
                    AnalyzeMethod(type, calledMethod);
                    IList<FieldInfo> calledMethodFields;
                    methodFieldDependencies.TryGetValue(calledMethod, out calledMethodFields);

                    if (calledMethodFields != null)
                    {
                        foreach (var calledMethodField in calledMethodFields)
                        {
                            fieldList.AddIfNew(calledMethodField);
                        }
                    }
                }
            }

        }
    }
}