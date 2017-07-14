namespace QuickInject
{
    using System;
    using System.Collections.Generic;

    public abstract class InjectionMember
    {
        public Type ResultType { get; protected set; }

        public IEnumerable<Type> DependentTypes { get; protected set; }

        public ICodeProvider CodeProvider { get; protected set; }
    }
}