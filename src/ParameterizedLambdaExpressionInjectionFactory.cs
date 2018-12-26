// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
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

#pragma warning disable SA1402 // File may only contain a single class
    public sealed class ParameterizedLambdaExpressionInjectionFactory<T1, TResult> : InjectionMember
#pragma warning restore SA1402 // File may only contain a single class
    {
        public ParameterizedLambdaExpressionInjectionFactory(ICodeProvider codeProvider = null)
        {
            this.ResultType = typeof(TResult);
            this.DependentTypes = new List<Type> { typeof(T1) };
            this.CodeProvider = codeProvider;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public sealed class ParameterizedLambdaExpressionInjectionFactory<T1, T2, TResult> : InjectionMember
#pragma warning restore SA1402 // File may only contain a single class
    {
        public ParameterizedLambdaExpressionInjectionFactory(ICodeProvider codeProvider = null)
        {
            this.ResultType = typeof(TResult);
            this.DependentTypes = new List<Type> { typeof(T1), typeof(T2) };
            this.CodeProvider = codeProvider;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public sealed class ParameterizedLambdaExpressionInjectionFactory<T1, T2, T3, TResult> : InjectionMember
#pragma warning restore SA1402 // File may only contain a single class
    {
        public ParameterizedLambdaExpressionInjectionFactory(ICodeProvider codeProvider = null)
        {
            this.ResultType = typeof(TResult);
            this.DependentTypes = new List<Type> { typeof(T1), typeof(T2), typeof(T3) };
            this.CodeProvider = codeProvider;
        }
    }

#pragma warning disable SA1402 // File may only contain a single class
    public sealed class ParameterizedLambdaExpressionInjectionFactory<T1, T2, T3, T4, TResult> : InjectionMember
#pragma warning restore SA1402 // File may only contain a single class
    {
        public ParameterizedLambdaExpressionInjectionFactory(ICodeProvider codeProvider = null)
        {
            this.ResultType = typeof(TResult);
            this.DependentTypes = new List<Type> { typeof(T1), typeof(T2), typeof(T3), typeof(T4) };
            this.CodeProvider = codeProvider;
        }
    }
}