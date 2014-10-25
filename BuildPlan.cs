namespace QuickInject
{
    using System;

    internal sealed class BuildPlan
    {
        public bool IsCompiled { get; set; }

        public Func<object> Expression { get; set; }
    }
}