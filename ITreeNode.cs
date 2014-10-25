namespace QuickInject
{
    using System;
    using System.Collections.Generic;

    public interface ITreeNode<T> : IEnumerable<ITreeNode<T>>
    {
        ITreeNode<T> Parent { get; }

        T Value { get; }

        IReadOnlyCollection<ITreeNode<T>> Children { get; }

        ITreeNode<T> this[int index] { get; }

        ITreeNode<T> AddChild(T value);

        bool RemoveChild(ITreeNode<T> node);

        void Traverse(Action<T> action);

        IEnumerable<T> Flatten();
    }
}