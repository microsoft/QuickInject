namespace QuickInject
{
    using System;

    public interface IQuickInjectContainer
    {
        void AddBuildPlanVisitor(IBuildPlanVisitor visitor);

        void RegisterDependencyTreeListener(Action<ITreeNode<Type>> root);
    }
}