﻿// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved. See License.txt in the project root for license information.

// This is https://github.com/mono/rx/blob/master/Rx/NET/Source/System.Reactive.Core/Reactive/Internal/PriorityQueue.cs

using System;
using System.Threading;
using System.Collections.Generic;

namespace Punchclock
{
    class PriorityQueue<T> where T : IComparable<T>
    {
#if !NO_INTERLOCKED_64
        static long _count = long.MinValue;
#else
        static int _count = int.MinValue;
#endif
        IndexedItem[] _items;
        int _size;

        const int DEFAULT_CAPACITY = 16;
        
        public PriorityQueue()
            : this(DEFAULT_CAPACITY)
        {
        }

        public PriorityQueue(int capacity)
        {
            _items = new IndexedItem[capacity];
            _size = 0;
        }

        bool IsHigherPriority(int left, int right)
        {
            return _items[left].CompareTo(_items[right]) < 0;
        }

        void Percolate(int index)
        {
            if (index >= _size || index < 0)
                return;
            var parent = (index - 1) / 2;
            if (parent < 0 || parent == index)
                return;

            if (IsHigherPriority(index, parent))
            {
                var temp = _items[index];
                _items[index] = _items[parent];
                _items[parent] = temp;
                Percolate(parent);
            }
        }

        void Heapify()
        {
            Heapify(0);
        }

        void Heapify(int index)
        {
            if (index >= _size || index < 0)
                return;

            var left = 2 * index + 1;
            var right = 2 * index + 2;
            var first = index;

            if (left < _size && IsHigherPriority(left, first))
                first = left;
            if (right < _size && IsHigherPriority(right, first))
                first = right;
            if (first != index)
            {
                var temp = _items[index];
                _items[index] = _items[first];
                _items[first] = temp;
                Heapify(first);
            }
        }

        public int Count { get { return _size; } }

        public T Peek()
        {
            if (_size == 0)
                throw new InvalidOperationException("There are no items in the collection");

            return _items[0].Value;
        }

        void RemoveAt(int index, bool single)
        {
            _items[index] = _items[--_size];
            _items[_size] = default(IndexedItem);
            Heapify();
            if (_size < _items.Length / 4 && (single || _size < DEFAULT_CAPACITY))
            {
                var temp = _items;
                _items = new IndexedItem[_items.Length / 2];
                Array.Copy(temp, 0, _items, 0, _size);
            }
        }

        public T Dequeue()
        {
            var result = Peek();
            RemoveAt(0, true);
            return result;
        }
        
        public T[] DequeueSome(int count)
        {
            if (count == 0) {
                return new T[0];
            }

            var ret = new T[count];
            count = Math.Min(count, _size);
            for (int i = 0; i < count; i++) {
                ret[i] = Peek();
                RemoveAt(0, false);
            }

            return ret;
        }
        
        public T[] DequeueAll()
        {
            return DequeueSome(_size);
        }
        
        public void Enqueue(T item)
        {
            if (_size >= _items.Length)
            {
                var temp = _items;
                _items = new IndexedItem[_items.Length * 2];
                Array.Copy(temp, _items, temp.Length);
            }

            var index = _size++;
            _items[index] = new IndexedItem { Value = item, Id = Interlocked.Increment(ref _count) };
            Percolate(index);
        }

        public bool Remove(T item)
        {
            for (var i = 0; i < _size; ++i)
            {
                if (EqualityComparer<T>.Default.Equals(_items[i].Value, item))
                {
                    RemoveAt(i, false);
                    return true;
                }
            }

            return false;
        }

        struct IndexedItem : IComparable<IndexedItem>
        {
            public T Value;
#if !NO_INTERLOCKED_64
            public long Id;
#else
            public int Id;
#endif

            public int CompareTo(IndexedItem other)
            {
                var c = Value.CompareTo(other.Value);
                if (c == 0)
                    c = Id.CompareTo(other.Id);
                return c;
            }
        }
    }
}
