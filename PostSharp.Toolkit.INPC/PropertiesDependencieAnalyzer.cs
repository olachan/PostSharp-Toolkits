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

            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => !p.GetCustomAttributes(typeof(NoAutomaticPropertyChangedNotificationsAttribute), true).Any());

            foreach (var propertyInfo in properties)
            {
                if (!propertyInfo.CanRead) continue;

                methodAnalyzer.AnalyzeProperty( type, propertyInfo );

                IList<FieldInfo> fieldList;

                //Time to build the reversed graph, i.e. field->property dependencies

                if (methodAnalyzer.MethodFieldDependencies.TryGetValue(propertyInfo.GetGetMethod(), out fieldList))
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
            private class AnalysisContext : NestableContextInfo
            {
                public Type CurrentType { get; set; }

                public MethodBase CurrentMethod { get; set; }

                public PropertyInfo CurrentProperty { get; set; }

                public AnalysisContext()
                { }

                public AnalysisContext( Type currentType, MethodBase currentMethod, PropertyInfo currentProperty )
                {
                    this.CurrentType = currentType;
                    this.CurrentMethod = currentMethod;
                    this.CurrentProperty = currentProperty;
                }

                public AnalysisContext CloneWithDifferentMethod(MethodBase method)
                {
                    return new AnalysisContext()
                               {
                                   CurrentMethod = method,
                                   CurrentProperty = this.CurrentProperty,
                                   CurrentType = this.CurrentType
                               };
                }
            }


            private readonly NestableContext<AnalysisContext> context = new NestableContext<AnalysisContext>();
            
            //Methods already analyzed (for redundant analysis and cycles avoidance)
            private readonly HashSet<MethodBase> analyzedMethods = new HashSet<MethodBase>();

            //Dependencies of methods on fields
            private readonly Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies = new Dictionary<MethodBase, IList<FieldInfo>>();

            public Dictionary<MethodBase, IList<FieldInfo>> MethodFieldDependencies
            {
                get { return this.methodFieldDependencies; }
            }

            private readonly ISyntaxService syntaxService;

            public MethodAnalyzer(PropertiesDependencieAnalyzer analyzer)
            {
                syntaxService = PostSharpEnvironment.CurrentProject.GetService<ISyntaxService>();
            }

            public void AnalyzeProperty(Type type, PropertyInfo propertyInfo)
            {
                if (context.Current != null)
                {
                    throw new NotSupportedException("MethodAnalyzer is currently single-threaded!");
                }

                var propertyGetter = propertyInfo.GetGetMethod(false);

                if (propertyGetter == null)
                {
                    return;
                }

                using (this.context.InContext(() => new AnalysisContext(type, propertyGetter, propertyInfo)))
                {
                    this.AnalyzeMethodRecursive(propertyGetter);
                }
            }

            private void AnalyzeMethodRecursive( MethodBase method )
            {
                if ( this.analyzedMethods.Contains( method ) )
                {
                    return;
                }

                this.analyzedMethods.Add( method );

                using (this.context.InContext(() => this.context.Current.CloneWithDifferentMethod(method)))
                {
                    //TODO: Any better way to get MethodDefDeclaration?
                    MethodDefDeclaration methodDef =
                        ((Project) PostSharpEnvironment.CurrentProject).Module.FindMethod( method, BindingOptions.Default ).GetMethodDefinition();

                    var body = syntaxService.GetMethodBody( methodDef, SyntaxAbstractionLevel.ExpressionTree );

                    this.VisitMethodBody( body );
                }
            }

            public override object VisitFieldExpression(IFieldExpression expression)
            {
                if (expression.Instance.SyntaxElementKind != SyntaxElementKind.This)
                {
                    //TODO: Write tests for build-time errors and warnings!
                    InpcMessageSource.Instance.Write( this.context.Current.CurrentProperty, SeverityType.Error, "INPC001",
                        context.Current.CurrentProperty, context.Current.CurrentMethod );
                }

                methodFieldDependencies.GetOrCreate(context.Current.CurrentMethod, () => new List<FieldInfo>()).AddIfNew(expression.Field);

                return base.VisitFieldExpression(expression);
            }

            

            public override object VisitMethodCallExpression(IMethodCallExpression expression)
            {
                MethodInfo methodInfo = (MethodInfo)expression.Method;

                // Ignore void no ref/out methods, static framework methods and independent methods

                if ((expression.Instance == null || expression.Instance.SyntaxElementKind != SyntaxElementKind.This) &&
                    (methodInfo.IsVoidNoRefOut() || methodInfo.IsStateIndependentMethod() || methodInfo.IsFrameworkStaticMethod()))
                {
                    return base.VisitMethodCallExpression(expression);
                }

                if (expression.Instance == null || expression.Instance.SyntaxElementKind != SyntaxElementKind.This)
                {
                    //TODO: Write tests for build-time errors and warnings!
                    InpcMessageSource.Instance.Write(this.context.Current.CurrentProperty, SeverityType.Error, "INPC002",
                       context.Current.CurrentProperty, context.Current.CurrentMethod);
                    
                    return base.VisitMethodCallExpression(expression);
                }

                this.AnalyzeMethodRecursive(expression.Method);
                IList<FieldInfo> calledMethodFields;
                this.methodFieldDependencies.TryGetValue(expression.Method, out calledMethodFields);

                if (calledMethodFields != null)
                {
                    var fieldList = methodFieldDependencies.GetOrCreate(context.Current.CurrentMethod, () => new List<FieldInfo>());
                    foreach (var calledMethodField in calledMethodFields)
                    {
                        fieldList.AddIfNew(calledMethodField);
                    }
                }

                return base.VisitMethodCallExpression(expression);
            }
            
            public override object VisitMethodPointerExpression(IMethodPointerExpression expression)
            {
                InpcMessageSource.Instance.Write(this.context.Current.CurrentProperty, SeverityType.Error, "INPC003",
                      context.Current.CurrentProperty, context.Current.CurrentMethod);
                return base.VisitMethodPointerExpression(expression);
            }

        }
    }

    
}