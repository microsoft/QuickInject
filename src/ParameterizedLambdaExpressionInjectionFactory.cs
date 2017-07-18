namespace QuickInject
{
    using System;
    using System.Collections.Generic;

    public sealed class ParameterizedLambdaExpressionInjectionFactory<TResult> : InjectionMember
    {
        public ParameterizedLambdaExpressionInjectionFactory(ICodeProvider codeProvider = null)
        {
            this.ResultType = typeof(TResult);
            this.DependentTypes = new List<Type>();
            this.CodeProvider = codeProvider;
        }
    }

    public sealed class ParameterizedLambdaExpressionInjectionFactory<T1, TResult> : InjectionMember
    {
        public ParameterizedLambdaExpressionInjectionFactory(ICodeProvider codeProvider = null)
        {
            this.ResultType = typeof(TResult);
            this.DependentTypes = new List<Type> { typeof(T1) };
            this.CodeProvider = codeProvider;
        }
    }

    public sealed class ParameterizedLambdaExpressionInjectionFactory<T1, T2, TResult> : InjectionMember
    {
        public ParameterizedLambdaExpressionInjectionFactory(ICodeProvider codeProvider = null)
        {
            this.ResultType = typeof(TResult);
            this.DependentTypes = new List<Type> { typeof(T1), typeof(T2) };
            this.CodeProvider = codeProvider;
        }
    }

    public sealed class ParameterizedLambdaExpressionInjectionFactory<T1, T2, T3, TResult> : InjectionMember
    {
        public ParameterizedLambdaExpressionInjectionFactory(ICodeProvider codeProvider = null)
        {
            this.ResultType = typeof(TResult);
            this.DependentTypes = new List<Type> { typeof(T1), typeof(T2), typeof(T3) };
            this.CodeProvider = codeProvider;
        }
    }

    public sealed class ParameterizedLambdaExpressionInjectionFactory<T1, T2, T3, T4, TResult> : InjectionMember
    {
        public ParameterizedLambdaExpressionInjectionFactory(ICodeProvider codeProvider = null)
        {
            this.ResultType = typeof(TResult);
            this.DependentTypes = new List<Type> { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
            this.CodeProvider = codeProvider;
        }
    }
}