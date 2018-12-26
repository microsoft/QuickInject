// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.QuickInject
{
    using System;

    internal struct GrowableArray<T>
    {
        private int arrayLength;

        public GrowableArray(int initialSize)
        {
            this.UnderlyingArray = new T[initialSize];
            this.arrayLength = 0;
        }

        public int Count
        {
            get => this.arrayLength;
            set
            {
                if (this.arrayLength < value)
                {
                    if (this.UnderlyingArray != null && value <= this.UnderlyingArray.Length)
                    {
                        for (int i = this.arrayLength; i < value; i++)
                        {
                            this.UnderlyingArray[i] = default(T);
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

        public T[] UnderlyingArray { get; private set; }

        public T this[int index]
        {
            get => this.UnderlyingArray[index];
            set => this.UnderlyingArray[index] = value;
        }

        public void Add(T item)
        {
            if (this.UnderlyingArray == null || this.arrayLength >= this.UnderlyingArray.Length)
            {
                this.Realloc(0);
            }

            this.UnderlyingArray[this.arrayLength++] = item;
        }

        private void Realloc(int minSize)
        {
            if (this.UnderlyingArray == null)
            {
                if (minSize < 16)
                {
                    minSize = 16;
                }

                this.UnderlyingArray = new T[minSize];
            }
            else
            {
                int expandSize = (this.UnderlyingArray.Length * 3 / 2) + 8;
                if (minSize < expandSize)
                {
                    minSize = expandSize;
                }

                var newArray = new T[minSize];
                Array.Copy(this.UnderlyingArray, newArray, this.arrayLength);
                this.UnderlyingArray = newArray;
            }
        }
    }
}