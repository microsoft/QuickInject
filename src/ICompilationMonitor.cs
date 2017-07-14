namespace QuickInject
{
    using System;

    public interface ICompilationMonitor
    {
        void Begin(Type t, object resolutionContext);

        void End(Type t, object resolutionContext);
    }
}