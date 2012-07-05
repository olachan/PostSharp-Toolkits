#region Copyright (c) 2012 by SharpCrafters s.r.o.
// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt
#endregion

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using PostSharp.Extensibility;
using PostSharp.Reflection;
using PostSharp.Reflection.Syntax;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.ReflectionWrapper;
using PostSharp.Sdk.CodeModel.Syntax;
using PostSharp.Sdk.Extensibility;

namespace PostSharp.Toolkit.INPC
{
    //TODO: Serious refactoring

    public class PropertiesDependencieAnalyzer
    {
        private readonly Dictionary<string, IList<string>> fieldDependentProperties = new Dictionary<string, IList<string>>();

        private MethodAnalyzer methodAnalyzer;

        public PropertiesDependencieAnalyzer()
        {
            methodAnalyzer = new MethodAnalyzer( this );
        }

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

                methodAnalyzer.AnalyzeProperty( type, getMethod );

                IList<FieldInfo> fieldList;

                //Time to build the reversed graph, i.e. field->property dependencies

                if (methodAnalyzer.MethodFieldDependencies.TryGetValue(getMethod, out fieldList))
                {
                    foreach (var field in fieldList)
                    {
                        IList<string> propertyList = fieldDependentProperties.GetOrCreate(string.Format("{0}.{1}", field.DeclaringType.FullName, field.Name), () => new List<string>());

                        propertyList.AddIfNew(propertyInfo.Name);
                    }

                }
            }
        }


   
        private class MethodAnalyzer : SyntaxTreeVisitor
        {
            private Type currentType;
            //private MethodBase currentMethod;

            private readonly PropertiesDependencieAnalyzer analyzer;

            //Methods already analyzed (for redundant analysis and cycles avoidance)
            private readonly HashSet<MethodBase> analyzedMethods = new HashSet<MethodBase>();

            //Dependencies of methods on fields
            private readonly Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies = new Dictionary<MethodBase, IList<FieldInfo>>();

            public Dictionary<MethodBase, IList<FieldInfo>> MethodFieldDependencies
            {
                get { return this.methodFieldDependencies; }
            }

            private MethodBase currentMethod;
            private ISyntaxService syntaxService;

            public MethodAnalyzer(PropertiesDependencieAnalyzer analyzer)
            {
                this.analyzer = analyzer;
                syntaxService = PostSharpEnvironment.CurrentProject.GetService<ISyntaxService>();
            }

            public void AnalyzeProperty(Type type, MethodBase propertyGetter)
            {
                if (currentType != null)
                {
                    throw new NotSupportedException("MethodAnalyzer is currently single-threaded!");
                }
                this.currentType = type;

                try
                {
                    this.AnalyzeMethodRecursive( propertyGetter );
                }
                finally
                {
                    currentType = null;
                }
            }

            private void AnalyzeMethodRecursive( MethodBase method )
            {
                if ( this.analyzedMethods.Contains( method ) )
                {
                    return;
                }

                this.analyzedMethods.Add( method );

                MethodBase prevMethod = this.currentMethod;
                this.currentMethod = method;

                try
                {
                    //TODO: Any better way to get MethodDefDeclaration?
                    MethodDefDeclaration methodDef =
                        ((Project) PostSharpEnvironment.CurrentProject).Module.FindMethod( method, BindingOptions.Default ).GetMethodDefinition();

                    var body = syntaxService.GetMethodBody( methodDef, SyntaxAbstractionLevel.ExpressionTree );

                    this.VisitMethodBody( body );
                }
                finally
                {
                    this.currentMethod = prevMethod;
                }
            }

            public override object VisitFieldExpression(IFieldExpression expression)
            {
                if (expression.Instance.SyntaxElementKind != SyntaxElementKind.This)
                {
                    //TODO: Proper error handling
                    throw new Exception(this.currentType.ToString()+" "+this.currentMethod+" "+expression);
                }

                methodFieldDependencies.GetOrCreate( this.currentMethod, () => new List<FieldInfo>() ).AddIfNew( expression.Field );

                return base.VisitFieldExpression(expression);
            }

            public override object VisitMethodCallExpression(IMethodCallExpression expression)
            {
                if (expression.Instance.SyntaxElementKind != SyntaxElementKind.This)
                {
                    //TODO: Proper error handling
                    throw new Exception();
                }

                this.AnalyzeMethodRecursive(expression.Method);
                IList<FieldInfo> calledMethodFields;
                this.methodFieldDependencies.TryGetValue(expression.Method, out calledMethodFields);

                if (calledMethodFields != null)
                {
                    var fieldList = methodFieldDependencies.GetOrCreate( this.currentMethod, () => new List<FieldInfo>() );
                    foreach (var calledMethodField in calledMethodFields)
                    {
                        fieldList.AddIfNew(calledMethodField);
                    }
                }

                return base.VisitMethodCallExpression(expression);
            }
        }
    }
}