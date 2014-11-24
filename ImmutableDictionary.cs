/*
The MIT License (MIT)

Copyright (c) 2013 Maksim Volkau

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
*/

/* !!!! NOTE !!!! */
/* Originally appears as HashTree<K, V> -- I've renamed it to ImmutableDictionary */
/* !!!! NOTE !!!! */

namespace QuickInject
{
    using System;

    /// <summary>
    /// Immutable kind of http://en.wikipedia.org/wiki/AVL_tree, where actual node key is <typeparamref name="K"/> hash code.
    /// </summary>
    internal sealed class ImmutableDictionary<K, V>
    {
        public static readonly ImmutableDictionary<K, V> Empty = new ImmutableDictionary<K, V>();

        private readonly int _hashCode;

        private readonly K _key;

        private readonly V _value;

        private readonly KV<K, V>[] _conflicts;

        private readonly ImmutableDictionary<K, V> _leftChild;

        private readonly ImmutableDictionary<K, V> _rightChild;

        private readonly int _height;

        public bool IsEmpty { get { return _height == 0; } }

        public delegate V UpdateValue(V current, V added);

        public ImmutableDictionary<K, V> AddOrUpdate(K key, V value, UpdateValue updateValue = null)
        {
            return AddOrUpdate(key.GetHashCode(), key, value, updateValue ?? ReplaceValue);
        }

        public V GetValueOrDefault(K key, V defaultValue = default(V))
        {
            var t = this;
            var hash = key.GetHashCode();
            while (t._height != 0 && t._hashCode != hash)
                t = hash < t._hashCode ? t._leftChild : t._rightChild;
            return t._height != 0 && (ReferenceEquals(key, t._key) || key.Equals(t._key)) ? t._value
                : t.GetConflictedValueOrDefault(key, defaultValue);
        }

        private ImmutableDictionary() { }

        private ImmutableDictionary(int hashCode, K key, V value, KV<K, V>[] conficts, ImmutableDictionary<K, V> leftChild, ImmutableDictionary<K, V> rightChild)
        {
            _hashCode = hashCode;
            _key = key;
            _value = value;
            _conflicts = conficts;
            _leftChild = leftChild;
            _rightChild = rightChild;
            _height = 1 + (leftChild._height > rightChild._height ? leftChild._height : rightChild._height);
        }

        private static V ReplaceValue(V _, V added) { return added; }

        private ImmutableDictionary<K, V> AddOrUpdate(int hash, K key, V value, UpdateValue updateValue)
        {
            return _height == 0 ? new ImmutableDictionary<K, V>(hash, key, value, null, Empty, Empty)
                : (hash == _hashCode ? ResolveConflicts(key, value, updateValue)
                : (hash < _hashCode
                    ? With(_leftChild.AddOrUpdate(hash, key, value, updateValue), _rightChild)
                    : With(_leftChild, _rightChild.AddOrUpdate(hash, key, value, updateValue)))
                        .EnsureBalanced());
        }

        private ImmutableDictionary<K, V> ResolveConflicts(K key, V value, UpdateValue updateValue)
        {
            if (ReferenceEquals(_key, key) || _key.Equals(key))
                return new ImmutableDictionary<K, V>(_hashCode, key, updateValue(_value, value), _conflicts, _leftChild, _rightChild);

            if (_conflicts == null)
                return new ImmutableDictionary<K, V>(_hashCode, _key, _value, new[] { new KV<K, V>(key, value) }, _leftChild, _rightChild);

            var i = _conflicts.Length - 1;
            while (i >= 0 && !Equals(_conflicts[i].Key, _key)) i--;
            var conflicts = new KV<K, V>[i != -1 ? _conflicts.Length : _conflicts.Length + 1];
            Array.Copy(_conflicts, 0, conflicts, 0, _conflicts.Length);
            conflicts[i != -1 ? i : _conflicts.Length] = new KV<K, V>(key, i != -1 ? updateValue(_conflicts[i].Value, value) : value);
            return new ImmutableDictionary<K, V>(_hashCode, _key, _value, conflicts, _leftChild, _rightChild);
        }

        private V GetConflictedValueOrDefault(K key, V defaultValue)
        {
            if (_conflicts != null)
                for (var i = 0; i < _conflicts.Length; i++)
                    if (Equals(_conflicts[i].Key, key))
                        return _conflicts[i].Value;
            return defaultValue;
        }

        private ImmutableDictionary<K, V> EnsureBalanced()
        {
            var delta = _leftChild._height - _rightChild._height;
            return delta >= 2 ? With(_leftChild._rightChild._height - _leftChild._leftChild._height == 1 ? _leftChild.RotateLeft() : _leftChild, _rightChild).RotateRight()
                : (delta <= -2 ? With(_leftChild, _rightChild._leftChild._height - _rightChild._rightChild._height == 1 ? _rightChild.RotateRight() : _rightChild).RotateLeft()
                : this);
        }

        private ImmutableDictionary<K, V> RotateRight()
        {
            return _leftChild.With(_leftChild._leftChild, With(_leftChild._rightChild, _rightChild));
        }

        private ImmutableDictionary<K, V> RotateLeft()
        {
            return _rightChild.With(With(_leftChild, _rightChild._leftChild), _rightChild._rightChild);
        }

        private ImmutableDictionary<K, V> With(ImmutableDictionary<K, V> left, ImmutableDictionary<K, V> right)
        {
            return new ImmutableDictionary<K, V>(_hashCode, _key, _value, _conflicts, left, right);
        }

        private sealed class KV<K, V>
        {
            public readonly K Key;
            public readonly V Value;

            public KV(K key, V value)
            {
                Key = key;
                Value = value;
            }
        }
    }
}