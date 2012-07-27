﻿#region Copyright (c) 2012 by SharpCrafters s.r.o.

// Copyright (c) 2012, SharpCrafters s.r.o.
// All rights reserved.
// 
// For licensing terms, see file License.txt

#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using PostSharp.Extensibility;
using PostSharp.Reflection.Syntax;


namespace PostSharp.Toolkit.Domain
{
    /// <summary>
    /// Performs static code analysis to determine property dependencies. 
    /// Stores two global maps: 
    /// 1. mapping a method to fields that the method depends on, 
    /// 2. mapping a field to properties that depend upon the field. 
    /// For each analyzed type analyzer returns <see cref="ExplicitDependencyMap"/> build based on <see cref="DependsOnAttribute"/> declarations.
    /// </summary>
    internal class PropertiesDependencieAnalyzer
    {
        private readonly Dictionary<string, List<string>> fieldDependentProperties = new Dictionary<string, List<string>>();

        private MethodAnalyzer methodAnalyzer;

        public PropertiesDependencieAnalyzer()
        {
            this.methodAnalyzer = new MethodAnalyzer(this);
        }

        public Dictionary<MethodBase, IList<FieldInfo>> MethodFieldDependencies
        {
            get
            {
                return this.methodAnalyzer.MethodFieldDependencies;
            }
        }

        public Dictionary<string, List<string>> FieldDependentProperties
        {
            get
            {
                return this.fieldDependentProperties;
            }
        }

        public ExplicitDependencyMap AnalyzeType(Type type)
        {
            //We need to grab all the properties and build a map of their dependencies

            IEnumerable<PropertyInfo> allProperties =
                type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where( p => !p.GetCustomAttributes(typeof(NotifyPropertyChangedIgnoreAttribute), true).Any())
                .ToList();

            IEnumerable<PropertyInfo> propertiesForImplicitAnalasis = allProperties.Where(p => !p.GetCustomAttributes(typeof(DependsOnAttribute), false).Any());

            var propertiesForExplicitAnalasis = allProperties
                .Select(p => new { Property = p, DependsOnAttribute = p.GetCustomAttributes(typeof(DependsOnAttribute), false) })
                .Where(p => p.DependsOnAttribute.Any());

            // build ExplicitDependencyMap. We nedd it here to add invocation chains.
            var currentTypeExplicitDependencyMap = new ExplicitDependencyMap(
                propertiesForExplicitAnalasis.Select(p => new ExplicitDependency(p.Property.Name, p.DependsOnAttribute.SelectMany(d => ((DependsOnAttribute)d).Dependencies))));

            foreach (PropertyInfo propertyInfo in propertiesForImplicitAnalasis)
            {
                if (!propertyInfo.CanRead)
                {
                    continue;
                }

                this.methodAnalyzer.AnalyzeProperty(type, propertyInfo, currentTypeExplicitDependencyMap);

                IList<FieldInfo> fieldList;

                //Time to build the reversed graph, i.e. field->property dependencies

                if (this.methodAnalyzer.MethodFieldDependencies.TryGetValue(propertyInfo.GetGetMethod(), out fieldList))
                {
                    foreach (FieldInfo field in fieldList)
                    {
                        IList<string> propertyList =
                            this.fieldDependentProperties.GetOrCreate(
                                field.FullName(), () => new List<string>());

                        propertyList.AddIfNew(propertyInfo.Name);
                    }
                }
            }

            return currentTypeExplicitDependencyMap;
        }

        private class MethodAnalyzer : SyntaxTreeVisitor
        {
            private class AnalysisContext : NestableContextInfo
            {
                public Type CurrentType { get; private set; }

                public MethodBase CurrentMethod { get; private set; }

                public PropertyInfo CurrentProperty { get; private set; }

                public ExplicitDependencyMap ExplicitDependencyMap { get; private set; }


                private bool? isNotifyPropertyChangedSafeProperty;

                public bool IsNotifyPropertyChangedSafeProperty
                {
                    get
                    {
                        return this.isNotifyPropertyChangedSafeProperty ??
                               (this.isNotifyPropertyChangedSafeProperty = this.CurrentProperty.GetCustomAttributes(typeof(NotifyPropertyChangedSafeAttribute), false).Any()).Value;
                    }
                }

                public  AnalysisContext()
                {
                }

                public AnalysisContext(Type currentType, MethodBase currentMethod, PropertyInfo currentProperty, ExplicitDependencyMap explicitDependencyMap)
                {
                    this.CurrentType = currentType;
                    this.CurrentMethod = currentMethod;
                    this.CurrentProperty = currentProperty;
                    this.ExplicitDependencyMap = explicitDependencyMap;
                }

                public AnalysisContext CloneWithDifferentMethod(MethodBase method)
                {
                    return new AnalysisContext { CurrentMethod = method, CurrentProperty = this.CurrentProperty, CurrentType = this.CurrentType, ExplicitDependencyMap = this.ExplicitDependencyMap};
                }
            }

            private readonly NestableContext<AnalysisContext> context = new NestableContext<AnalysisContext>();

            //Methods already analyzed (for redundant analysis and cycles avoidance)
            private readonly HashSet<MethodBase> analyzedMethods = new HashSet<MethodBase>();

            //Dependencies of methods on fields
            private readonly Dictionary<MethodBase, IList<FieldInfo>> methodFieldDependencies = new Dictionary<MethodBase, IList<FieldInfo>>();

            public Dictionary<MethodBase, IList<FieldInfo>> MethodFieldDependencies
            {
                get
                {
                    return this.methodFieldDependencies;
                }
            }

            private readonly ISyntaxReflectionService syntaxService;

            public MethodAnalyzer(PropertiesDependencieAnalyzer analyzer)
            {
                this.syntaxService = PostSharpEnvironment.CurrentProject.GetService<ISyntaxReflectionService>();
            }

            public void AnalyzeProperty(Type type, PropertyInfo propertyInfo, ExplicitDependencyMap currentTypeExplicitDependencyMap)
            {
                if (this.context.Current != null)
                {
                    throw new NotSupportedException("MethodAnalyzer is currently single-threaded!");
                }

                MethodInfo propertyGetter = propertyInfo.GetGetMethod(false);

                if (propertyGetter == null)
                {
                    return;
                }

                using (this.context.InContext(() => new AnalysisContext(type, propertyGetter, propertyInfo, currentTypeExplicitDependencyMap)))
                {
                    this.AnalyzeMethodRecursive(propertyGetter);
                }
            }

            private void AnalyzeMethodRecursive(MethodBase method)
            {
                if (this.analyzedMethods.Contains(method))
                {
                    return;
                }

                this.analyzedMethods.Add(method);

                using (this.context.InContext(() => this.context.Current.CloneWithDifferentMethod(method)))
                {
                    ISyntaxMethodBody body = this.syntaxService.GetMethodBody(method, SyntaxAbstractionLevel.ExpressionTree);

                    if (body != null)
                    {
                        this.VisitMethodBody(body);
                    }
                }
            }

            public override object VisitFieldExpression(IFieldExpression expression)
            {
                //Check for access to static fields or fields of other objects
                if (expression.Instance == null || expression.Instance.SyntaxElementKind != SyntaxElementKind.This)
                {
                    // Method contains direct access to a field of another class.
                    if (!this.context.Current.IsNotifyPropertyChangedSafeProperty)
                    {
                        DomainMessageSource.Instance.Write(
                            this.context.Current.CurrentProperty,
                            SeverityType.Error,
                            "INPC001",
                            this.context.Current.CurrentProperty,
                            this.context.Current.CurrentMethod);
                    }

                    return base.VisitFieldExpression(expression);
                }


                this.methodFieldDependencies.GetOrCreate(this.context.Current.CurrentMethod, () => new List<FieldInfo>()).AddIfNew(expression.Field);

                return base.VisitFieldExpression(expression);
            }

            public override object VisitMethodCallExpression(IMethodCallExpression expression)
            {
                MethodInfo methodInfo = (MethodInfo)expression.Method;

                string invocationPath;

                // if expression is property invocation chain add explicite dependency and don't analyze this branch.
                if (this.context.Current.CurrentProperty.GetGetMethod() == this.context.Current.CurrentMethod && GetPropertyInvocationChain(expression, out invocationPath))
                {
                    this.context.Current.ExplicitDependencyMap.AddDependecy( this.context.Current.CurrentProperty.Name, invocationPath );
                    return base.VisitMethodCallExpression(expression);
                }

                // Ignore void no ref/out, Idempotent, InpcIgnored methods
                if (methodInfo.IsObjectToString() || methodInfo.IsVoidNoRefOut() || methodInfo.IsInpcIgnoredMethod() ||
                    (methodInfo.IsIdempotentMethod() && expression.Arguments.All(e => e.ReturnType.IsIntrinsic() || e.ReturnType.IsIntrinsicOrObjectArray())))
                {
                    return base.VisitMethodCallExpression(expression);
                }

                // Ignore static framework idempotent methods
                // TODO: For VoidNoRefOut method we should also check that arguments are intrinsic
                // TODO: The fact the we always accept ToString is risky
                if ((expression.Instance == null || expression.Instance.SyntaxElementKind != SyntaxElementKind.This) &&
                    (methodInfo.IsFrameworkStaticMethod() && expression.Arguments.All(e => e.ReturnType.IsIntrinsic() || e.ReturnType.IsIntrinsicOrObjectArray())))
                {
                    return base.VisitMethodCallExpression(expression);
                }

                //Check for method calls on external objects
                if (expression.Instance == null || expression.Instance.SyntaxElementKind != SyntaxElementKind.This)
                {
                    // emit error only fi we are not in NotifyPropertyChangedSafeProperty
                    if (!this.context.Current.IsNotifyPropertyChangedSafeProperty)
                    {
                        // Method contains call to non void (ref/out param) method of another class.
                        DomainMessageSource.Instance.Write(
                           this.context.Current.CurrentProperty,
                           SeverityType.Error,
                           "INPC002",
                           this.context.Current.CurrentProperty,
                           this.context.Current.CurrentMethod);
                    }

                    return base.VisitMethodCallExpression(expression); // End analysis of this branch
                }

                this.AnalyzeMethodRecursive(expression.Method);
                IList<FieldInfo> calledMethodFields;
                this.methodFieldDependencies.TryGetValue(expression.Method, out calledMethodFields);

                if (calledMethodFields != null)
                {
                    IList<FieldInfo> fieldList = this.methodFieldDependencies.GetOrCreate(this.context.Current.CurrentMethod, () => new List<FieldInfo>());
                    foreach (FieldInfo calledMethodField in calledMethodFields)
                    {
                        fieldList.AddIfNew(calledMethodField);
                    }
                }

                return base.VisitMethodCallExpression(expression);
            }

            private bool GetPropertyInvocationChain(IMethodCallExpression expression, out string invocationPath)
            {
                invocationPath = null;
                Stack<string> invocationStack = new Stack<string>();
                IExpression currentExpression = expression;

                while ( currentExpression is IMethodCallExpression && currentExpression.SyntaxElementKind != SyntaxElementKind.This)
                {
                    IMethodCallExpression methodCallExpression = (IMethodCallExpression)currentExpression;
                    if (!(methodCallExpression.Method.IsSpecialName && methodCallExpression.Method.Name.StartsWith( "get_" )))
                    {
                        return false;
                    }

                    invocationStack.Push( ((IMethodCallExpression)currentExpression).Method.Name.Substring( 4 ) );

                    currentExpression = methodCallExpression.Instance;
                }

                if (currentExpression.SyntaxElementKind != SyntaxElementKind.This && currentExpression.SyntaxElementKind != SyntaxElementKind.Field)
                {
                    return false;
                }

                IFieldExpression fieldExpression = currentExpression as IFieldExpression;

                if (fieldExpression != null)
                {
                    invocationStack.Push( fieldExpression.Field.Name );
                }

                invocationPath = invocationStack.Aggregate(new StringBuilder(), (builder, s) => builder.Append('.').Append(s)).Remove(0, 1).ToString();

                return true;
            }

            public override object VisitMethodPointerExpression(IMethodPointerExpression expression)
            {
                if (this.context.Current.IsNotifyPropertyChangedSafeProperty)
                {
                    return base.VisitMethodPointerExpression(expression);
                }

                // Method contains delegate call.
                DomainMessageSource.Instance.Write(
                    this.context.Current.CurrentProperty,
                    SeverityType.Error,
                    "INPC003",
                    this.context.Current.CurrentProperty,
                    this.context.Current.CurrentMethod);
                return base.VisitMethodPointerExpression(expression);
            }
        }
    }
}