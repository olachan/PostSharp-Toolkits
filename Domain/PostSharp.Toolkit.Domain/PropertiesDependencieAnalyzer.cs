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
                .Where(p => !p.GetCustomAttributes(typeof(NotifyPropertyChangedIgnoreAttribute), true).Any())
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

                public static ExpressionValidatorRepository ValidatorRepository { get; set; }

                private bool? isNotifyPropertyChangedSafeProperty;

                public bool IsNotifyPropertyChangedSafeProperty
                {
                    get
                    {
                        return this.isNotifyPropertyChangedSafeProperty ??
                               (this.isNotifyPropertyChangedSafeProperty = this.CurrentProperty.GetCustomAttributes(typeof(NotifyPropertyChangedSafeAttribute), false).Any()).Value;
                    }
                }

                public ExpressionValidationResult Validate(IExpression expression)
                {
                    return ValidatorRepository.Validate(expression, this);
                }

                static AnalysisContext()
                {
                    ValidatorRepository = new ExpressionValidatorRepository();
                }

                public AnalysisContext()
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
                    return new AnalysisContext { CurrentMethod = method, CurrentProperty = this.CurrentProperty, CurrentType = this.CurrentType, ExplicitDependencyMap = this.ExplicitDependencyMap };
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
                ExpressionValidationResult validationResult = this.context.Current.Validate(expression);

                if (validationResult.HasFlag(ExpressionValidationResult.ImmediateReturn))
                {
                    return base.VisitFieldExpression(expression);
                }

                this.methodFieldDependencies.GetOrCreate(this.context.Current.CurrentMethod, () => new List<FieldInfo>()).AddIfNew(expression.Field);

                return base.VisitFieldExpression(expression);
            }

            public override object VisitMethodCallExpression(IMethodCallExpression expression)
            {
                string invocationPath;

                // if expression is property invocation chain add explicite dependency and don't analyze this branch.
                if (this.context.Current.CurrentProperty.GetGetMethod() == this.context.Current.CurrentMethod && 
                    GetPropertyInvocationChain(expression, out invocationPath))
                {
                    this.context.Current.ExplicitDependencyMap.AddDependecy(this.context.Current.CurrentProperty.Name, invocationPath);
                    return base.VisitMethodCallExpression(expression);
                }

                ExpressionValidationResult validationResult = this.context.Current.Validate(expression);

                if (validationResult.HasFlag( ExpressionValidationResult.ImmediateReturn ))
                {
                    return base.VisitMethodCallExpression(expression);
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

                while (currentExpression is IMethodCallExpression && currentExpression.SyntaxElementKind != SyntaxElementKind.This)
                {
                    IMethodCallExpression methodCallExpression = (IMethodCallExpression)currentExpression;
                    if (!(methodCallExpression.Method.IsSpecialName && methodCallExpression.Method.Name.StartsWith("get_")))
                    {
                        return false;
                    }

                    invocationStack.Push(((IMethodCallExpression)currentExpression).Method.Name.Substring(4));

                    currentExpression = methodCallExpression.Instance;
                }

                if (currentExpression.SyntaxElementKind != SyntaxElementKind.This && currentExpression.SyntaxElementKind != SyntaxElementKind.Field)
                {
                    return false;
                }

                IFieldExpression fieldExpression = currentExpression as IFieldExpression;

                if (fieldExpression != null)
                {
                    invocationStack.Push(fieldExpression.Field.Name);
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

            private interface IMethodCallValidator
            {
                ExpressionValidationResultWithErrors ValidateMethod(IMethodCallExpression methodCallExpression, AnalysisContext currentContext);
            }

            private interface IFieldValidator
            {
                ExpressionValidationResultWithErrors ValidateField(IFieldExpression fieldExpression, AnalysisContext currentContext);
            }

            private sealed class ExpressionValidatorRepository
            {
                private readonly List<IMethodCallValidator> methodCallValidators;
                private readonly List<IFieldValidator> fieldValidators;


                public ExpressionValidatorRepository()
                {
                    this.methodCallValidators = new List<IMethodCallValidator>()
                        {
                            // order has impact on performance - when we encounter first validator that returns AcceptImmediateReturn we stop the validation process
                            new GenericMethodInfoPropertyValidator(  
                                methodInfo => methodInfo.IsVoidNoRefOut(),
                                methodInfo => methodInfo.IsObjectToString(),
                                methodInfo => methodInfo.IsObjectGetHashCode(),
                                methodInfo => methodInfo.IsInpcIgnoredMethod(),
                                methodInfo => methodInfo.DeclaringType == typeof(StringBuilder)),
                            new IdempotentMethodValidator(),
                            new OuterScopeObjectMethodCallValidator()
                        };

                    this.fieldValidators = new List<IFieldValidator>()
                        {
                            new OuterScopeObjectFieldValidator()
                        };
                }

                public ExpressionValidationResult Validate(IExpression expression, AnalysisContext currentContext)
                {
                    IMethodCallExpression methodCallExpression = expression as IMethodCallExpression;
                    if ( methodCallExpression != null )
                    {
                        return this.ProcessResults(
                            this.methodCallValidators.Select( v => v.ValidateMethod( methodCallExpression, currentContext ) ), currentContext );
                    }

                    IFieldExpression fieldExpression = expression as IFieldExpression;
                    if (fieldExpression != null)
                    {
                        return this.ProcessResults(
                            this.fieldValidators.Select(v => v.ValidateField(fieldExpression, currentContext)), currentContext);
                    }

                    return ExpressionValidationResult.Abstain;
                }

                private ExpressionValidationResult ProcessResults(IEnumerable<ExpressionValidationResultWithErrors> validationResults, AnalysisContext currentContext)
                {
                    Lazy<List<ExpressionValidationResultWithErrors>> rejectedResults = new Lazy<List<ExpressionValidationResultWithErrors>>();
                    foreach (ExpressionValidationResultWithErrors result in validationResults)
                    {
                        if (result.Result == ExpressionValidationResult.AcceptImmediateReturn)
                        {
                            return ExpressionValidationResult.AcceptImmediateReturn;
                        }

                        if (result.Result.HasFlag( ExpressionValidationResult.Reject ))
                        {
                            rejectedResults.Value.Add( result );
                        }
                    }

                    if (!rejectedResults.IsValueCreated)
                    {
                        return ExpressionValidationResult.Accept;
                    }

                    bool immediateReturn = false;

                    foreach ( ExpressionValidationResultWithErrors resultWithErrors in rejectedResults.Value )
                    {
                        immediateReturn |= resultWithErrors.Result.HasFlag( ExpressionValidationResult.ImmediateReturn );

                        DomainMessageSource.Instance.Write(
                              currentContext.CurrentProperty,
                              resultWithErrors.SeverityType,
                              resultWithErrors.MessageId,
                              resultWithErrors.MessageArguments);
                    }

                    if (immediateReturn)
                    {
                        return ExpressionValidationResult.RejectImmediateReturn;
                    }

                    return ExpressionValidationResult.Reject;
                }
            }

             private sealed class OuterScopeObjectFieldValidator : IFieldValidator
             {
                 public ExpressionValidationResultWithErrors ValidateField( IFieldExpression fieldExpression, AnalysisContext currentContext )
                 {
                     //Check for access to static fields or fields of other objects
                     if (fieldExpression.Instance == null || fieldExpression.Instance.SyntaxElementKind != SyntaxElementKind.This)
                     {
                         // Method contains direct access to a field of another class.
                         if (!currentContext.IsNotifyPropertyChangedSafeProperty)
                         {
                             return new ExpressionValidationResultWithErrors(ExpressionValidationResult.RejectImmediateReturn)
                             {
                                 MessageId = "INPC001",
                                 SeverityType = SeverityType.Error,
                                 MessageArguments = new object[]
                                                     {
                                                        currentContext.CurrentProperty,
                                                        currentContext.CurrentMethod
                                                     }
                             };
                         }

                         return ExpressionValidationResultWithErrors.AcceptImidietReturn;
                     }

                     return ExpressionValidationResultWithErrors.Abstain;
                 }
             }

            private sealed class OuterScopeObjectMethodCallValidator : IMethodCallValidator
            {
                public ExpressionValidationResultWithErrors ValidateMethod(IMethodCallExpression methodCallExpression, AnalysisContext currentContext)
                {
                    if (methodCallExpression.Instance == null || methodCallExpression.Instance.SyntaxElementKind != SyntaxElementKind.This)
                    {
                        // emit error only fi we are not in NotifyPropertyChangedSafeProperty
                        if (!currentContext.IsNotifyPropertyChangedSafeProperty)
                        {
                            // Method contains call to non void (ref/out param) method of another class.

                            return new ExpressionValidationResultWithErrors(ExpressionValidationResult.RejectImmediateReturn)
                            {
                                MessageId = "INPC002",
                                SeverityType = SeverityType.Error,
                                MessageArguments = new object[]
                                                     {
                                                        currentContext.CurrentProperty,
                                                        currentContext.CurrentMethod
                                                     }
                            };
                        }

                        return ExpressionValidationResultWithErrors.AcceptImidietReturn;
                    }

                    return ExpressionValidationResultWithErrors.Abstain;
                }
            }

            private sealed class IdempotentMethodValidator : IMethodCallValidator
            {
                public ExpressionValidationResultWithErrors ValidateMethod(IMethodCallExpression methodCallExpression, AnalysisContext currentContext)
                {
                    MethodInfo methodInfo = methodCallExpression.Method as MethodInfo;
                    if (methodInfo == null)
                    {
                        return ExpressionValidationResultWithErrors.Abstain;
                    }

                    bool allargumentsAreIntrinsic = methodCallExpression.Arguments.All(e => e.ReturnType.IsIntrinsic() || e.ReturnType.IsIntrinsicOrObjectArray());

                    if (methodInfo.IsIdempotentMethod() && allargumentsAreIntrinsic)
                    {
                        return ExpressionValidationResultWithErrors.AcceptImidietReturn;
                    }


                    if ((methodCallExpression.Instance == null || methodCallExpression.Instance.SyntaxElementKind != SyntaxElementKind.This) &&
                        (methodInfo.IsFrameworkStaticMethod() && allargumentsAreIntrinsic))
                    {
                        return ExpressionValidationResultWithErrors.AcceptImidietReturn;
                    }

                    return ExpressionValidationResultWithErrors.Abstain;
                }
            }


            private sealed class GenericMethodInfoPropertyValidator : IMethodCallValidator
            {
                private readonly Func<MethodInfo, bool>[] predicates;

                public GenericMethodInfoPropertyValidator(params Func<MethodInfo, bool>[] predicates)
                {
                    this.predicates = predicates;
                }

                public ExpressionValidationResultWithErrors ValidateMethod(IMethodCallExpression methodCallExpression, AnalysisContext currentContext)
                {
                    MethodInfo methodInfo = methodCallExpression.Method as MethodInfo;
                    if (methodInfo == null)
                    {
                        return ExpressionValidationResultWithErrors.Abstain;
                    }

                    foreach (Func<MethodInfo, bool> predicate in predicates)
                    {
                        if (predicate(methodInfo))
                        {
                            return ExpressionValidationResultWithErrors.AcceptImidietReturn;
                        }
                    }

                    return ExpressionValidationResultWithErrors.Abstain;
                }
            }

            [Flags]
            private enum ExpressionValidationResult
            {
                Accept = 0x01,
                ImmediateReturn = 0x02,
                Reject = 0x04,
                Abstain = 0x08,
                AcceptImmediateReturn = Accept | ImmediateReturn,
                RejectImmediateReturn = Reject | ImmediateReturn
            }

            private sealed class ExpressionValidationResultWithErrors
            {
                public static readonly ExpressionValidationResultWithErrors AcceptImidietReturn = new ExpressionValidationResultWithErrors(ExpressionValidationResult.AcceptImmediateReturn);
                public static readonly ExpressionValidationResultWithErrors Abstain = new ExpressionValidationResultWithErrors(ExpressionValidationResult.Abstain);

                public ExpressionValidationResultWithErrors(ExpressionValidationResult result)
                {
                    this.Result = result;
                }

                public ExpressionValidationResult Result { get; set; }

                public string MessageId { get; set; }

                public object[] MessageArguments { get; set; }

                public SeverityType SeverityType { get; set; }
            }
        }
    }


}