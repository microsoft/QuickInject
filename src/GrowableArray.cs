namespace QuickInject
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal struct GrowableArray<T>
    {
        private T[] array;

        private int arrayLength;

        public GrowableArray(int initialSize)
        {
            this.array = new T[initialSize];
            this.arrayLength = 0;
        }

        public int Count
        {
            get => this.arrayLength;
            set
            {
                if (this.arrayLength < value)
                {
                    if (this.array != null && value <= this.array.Length)
                    {
                        for (int i = this.arrayLength; i < value; i++)
                        {
                            this.array[i] = default(T);
                        }
                    }
                    else
                    {
                        this.Realloc(value);
                    }
                }

                this.arrayLength = value;
            }
        }

        public bool Empty => this.arrayLength == 0;

        public T Top => this.array[this.arrayLength - 1];

        public T[] UnderlyingArray => this.array;

        public bool EmptyCapacity => this.array == null;

        public T this[int index]
        {
            get => this.array[index];
            set => this.array[index] = value;
        }

        public void Clear()
        {
            this.arrayLength = 0;
        }

        public void Add(T item)
        {
            if (this.array == null || this.arrayLength >= this.array.Length)
            {
                this.Realloc(0);
            }

            this.array[this.arrayLength++] = item;
        }

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                this.Add(item);
            }
        }

        public void Insert(int index, T item)
        {
            if ((uint)index > (uint)this.arrayLength)
            {
                throw new IndexOutOfRangeException();
            }

            if (this.array == null || this.arrayLength >= this.array.Length)
            {
                this.Realloc(0);
            }

            for (int idx = this.arrayLength; index < idx; --idx)
            {
                this.array[idx] = this.array[idx - 1];
            }

            this.array[index] = item;
            this.arrayLength++;
        }

        public void RemoveRange(int index, int count)
        {
            if (count == 0)
            {
                return;
            }

            if (count < 0)
            {
                throw new ArgumentException("count can't be negative");
            }

            if ((uint)index >= (uint)this.arrayLength)
            {
                throw new IndexOutOfRangeException();
            }

            for (int endIndex = index + count; endIndex < this.arrayLength; endIndex++)
            {
                this.array[index++] = this.array[endIndex];
            }

            this.arrayLength = index;
        }

        public void Set(int index, T value)
        {
            if (index >= this.Count)
            {
                this.Count = index + 1;
            }

            this[index] = value;
        }

        public T Get(int index)
        {
            if ((uint)index < (uint)this.arrayLength)
            {
                return this[index];
            }

            return default(T);
        }

        public T Pop()
        {
            var ret = this.array[this.arrayLength - 1];
            --this.arrayLength;
            return ret;
        }

        public void Trim(int maxWaste)
        {
            if (this.array?.Length > this.arrayLength + maxWaste)
            {
                if (this.arrayLength == 0)
                {
                    this.array = null;
                }
                else
                {
                    var newArray = new T[this.arrayLength];
                    Array.Copy(this.array, newArray, this.arrayLength);
                    this.array = newArray;
                }
            }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("GrowableArray(Count=").Append(this.Count).Append(", [").AppendLine();

            for (int i = 0; i < this.Count; i++)
            {
                sb.Append("  ").Append(this[i]).AppendLine();
            }

            sb.Append("  ])");
            return sb.ToString();
        }

        public bool BinarySearch<Key>(Key key, out int index, Func<Key, T, int> comparison)
        {
            int low = 0;
            int high = this.arrayLength;
            int lastLowCompare = -1;

            if (high > 0)
            {
                while (true)
                {
                    int mid = (low + high) / 2;
                    int compareResult = comparison(key, this.array[mid]);
                    if (compareResult >= 0)
                    {
                        lastLowCompare = compareResult;

                        if (mid == low)
                        {
                            break;
                        }

                        low = mid;
                    }
                    else
                    {
                        high = mid;
                        if (mid == low)
                        {
                            break;
                        }
                    }
                }
            }

            if (lastLowCompare < 0)
            {
                --low;
            }

            index = low;

            return lastLowCompare == 0;
        }

        public void Sort(int index, int count, Comparison<T> comparison)
        {
            if (count > 0)
            {
                Array.Sort(this.array, index, count, new FunctorComparer<T>(comparison));
            }
        }

        public void Sort(Comparison<T> comparison)
        {
            if (this.array != null)
            {
                Array.Sort(this.array, 0, this.arrayLength, new FunctorComparer<T>(comparison));
            }
        }

        public GrowableArray<T1> Foreach<T1>(Func<T, T1> func)
        {
            var ret = new GrowableArray<T1>
            {
                Count = this.Count
            };

            for (int i = 0; i < this.Count; i++)
            {
                ret[i] = func(this.array[i]);
            }

            return ret;
        }

        public bool Search<Key>(Key key, int startIndex, Func<Key, T, int> compare, ref int index)
        {
            for (int i = startIndex; i < this.arrayLength; i++)
            {
                if (compare(key, this.array[i]) == 0)
                {
                    index = i;
                    return true;
                }
            }

            return false;
        }

        public GrowableArrayEnumerator GetEnumerator()
        {
            return new GrowableArrayEnumerator(this);
        }

        private void Realloc(int minSize)
        {
            if (this.array == null)
            {
                if (minSize < 16)
                {
                    minSize = 16;
                }

                this.array = new T[minSize];
            }
            else
            {
                int expandSize = (this.array.Length * 3 / 2) + 8;
                if (minSize < expandSize)
                {
                    minSize = expandSize;
                }

                var newArray = new T[minSize];
                Array.Copy(this.array, newArray, this.arrayLength);
                this.array = newArray;
            }
        }

        public struct GrowableArrayEnumerator
        {
            private readonly int end;

            private readonly T[] array;

            private int cur;

            internal GrowableArrayEnumerator(GrowableArray<T> growableArray)
            {
                this.cur = -1;
                this.end = growableArray.arrayLength;
                this.array = growableArray.array;
            }

            public T Current => this.array[this.cur];

            public bool MoveNext()
            {
                this.cur++;
                return this.cur < this.end;
            }
        }

        private sealed class FunctorComparer<K> : IComparer<K>
        {
            private readonly Comparison<K> comparison;

            public FunctorComparer(Comparison<K> comparison)
            {
                this.comparison = comparison;
            }

            public int Compare(K x, K y)
            {
                return this.comparison(x, y);
            }
        }
    }
}