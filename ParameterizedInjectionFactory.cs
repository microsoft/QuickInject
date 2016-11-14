// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace QuickInject
{
    using System;

    public sealed class ParameterizedInjectionFactory<TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, T8, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, TResult> func)
        {
            this.Factory = func.Method;
        }
    }

    public sealed class ParameterizedInjectionFactory<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> : InjectionMember
    {
        public ParameterizedInjectionFactory(Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, TResult> func)
        {
            this.Factory = func.Method;
        }
    }
}