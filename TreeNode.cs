namespace QuickInject
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;

    internal sealed class TreeNode<T> : ITreeNode<T>
    {
        private readonly T data;

        private readonly List<ITreeNode<T>> children = new List<ITreeNode<T>>();

        public TreeNode(T data)
        {
            this.data = data;
        }

        public ITreeNode<T> Parent { get; private set; }

        public T Value
        {
            get
            {
                return this.data;
            }
        }

        public IReadOnlyCollection<ITreeNode<T>> Children
        {
            get
            {
                return new ReadOnlyCollection<ITreeNode<T>>(this.children);
            }
        }

        public ITreeNode<T> this[int i]
        {
            get
            {
                return this.children[i];
            }
        }

        public ITreeNode<T> AddChild(T value)
        {
            var node = new TreeNode<T>(value) { Parent = this };
            this.children.Add(node);
            return node;
        }

        public bool RemoveChild(ITreeNode<T> node)
        {
            return this.children.Remove(node);
        }

        public void PreOrderTraverse(Action<T> action)
        {
            action(this.Value);
            foreach (var child in this.children)
            {
                child.PreOrderTraverse(action);
            }
        }

        public void PostOrderTraverse(Action<T> action)
        {
            foreach (var child in this.children)
            {
                child.PostOrderTraverse(action);
            }

            action(this.Value);
        }

        public IEnumerator<ITreeNode<T>> GetEnumerator()
        {
            yield return this;

            foreach (var directChild in this.children)
            {
                foreach (var anyChild in directChild)
                {
                    yield return anyChild;
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}