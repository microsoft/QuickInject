namespace QuickInject
{
    using System;

    public sealed class CyclicDependencyException<T> : Exception
    {
        private readonly T t;

        public CyclicDependencyException(T t)
        {
            this.t = t;
        }

        public override string Message
        {
            get
            {
                return "Cyclic dependency detected for " + t;
            }
        }
    }
}