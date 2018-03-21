﻿namespace Microsoft.ApplicationInsights.Metrics.ConcurrentDatastructures
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;

    /// <summary>ToDo: Complete documentation before stable release.</summary>
    /// <typeparam name="T">Type of collection elemets.</typeparam>
    internal class GrowingCollection<T> : IEnumerable<T>
    {
        private const int SegmentSize = 32;

        private Segment _dataHead;

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public GrowingCollection()
        {
            _dataHead = new Segment(null);
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                Segment currHead = Volatile.Read(ref _dataHead);
                return currHead.GlobalCount;
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <param name="item">ToDo: Complete documentation before stable release.</param>
        public void Add(T item)
        {
            Segment currHead = Volatile.Read(ref _dataHead);

            bool added = currHead.TryAdd(item);
            while (false == added)
            {
                Segment newHead = new Segment(currHead);
                Segment prevHead = Interlocked.CompareExchange(ref _dataHead, newHead, currHead);

                Segment updatedHead = (prevHead == currHead) ? newHead : prevHead;
                added = updatedHead.TryAdd(item);
            }
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public GrowingCollection<T>.Enumerator GetEnumerator()
        {
            var enumerator = new GrowingCollection<T>.Enumerator(_dataHead);
            return enumerator;
        }

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        /// <returns>ToDo: Complete documentation before stable release.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #region class Enumerator 

        /// <summary>ToDo: Complete documentation before stable release.</summary>
        public class Enumerator : IEnumerator<T>
        {
            private readonly Segment _head;
            private readonly int _headOffset;
            private readonly int _count;
            private Segment _currentSegment;
            private int _currentSegmentOffset;

            internal Enumerator(Segment head)
            {
                Util.ValidateNotNull(head, nameof(head));
                
                _head = _currentSegment = head;
                _headOffset = _currentSegmentOffset = head.LocalCount;
                _count = _headOffset + (_head.NextSegment == null ? 0 : _head.NextSegment.GlobalCount);
            }

            /// <summary>ToDo: Complete documentation before stable release.</summary>
            public int Count
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return _count;
                }
            }

            /// <summary>ToDo: Complete documentation before stable release.</summary>
            public T Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return _currentSegment[_currentSegmentOffset];
                }
            }

            object IEnumerator.Current
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return this.Current;
                }
            }

            /// <summary>ToDo: Complete documentation before stable release.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Dispose()
            {
            }

            /// <summary>ToDo: Complete documentation before stable release.</summary>
            /// <returns>ToDo: Complete documentation before stable release.</returns>
            public bool MoveNext()
            {
                if (_currentSegmentOffset == 0)
                {
                    if (_currentSegment.NextSegment == null)
                    {
                        return false;
                    }
                    else
                    {
                        _currentSegment = _currentSegment.NextSegment;
                        _currentSegmentOffset = _currentSegment.LocalCount - 1;
                        return true;
                    }
                }
                else
                {
                    _currentSegmentOffset--;
                    return true;
                }
            }

            /// <summary>ToDo: Complete documentation before stable release.</summary>
            public void Reset()
            {
                _currentSegment = _head;
                _currentSegmentOffset = _headOffset;
            }
        }
        #endregion class Enumerator 

        #region class Segment
        internal class Segment
        {
            private readonly Segment _nextSegment;
            private readonly int _nextSegmentGlobalCount;
            private readonly T[] _data = new T[SegmentSize];
            private int _localCount = 0;

            public Segment(Segment nextSegment)
            {
                _nextSegment = nextSegment;
                _nextSegmentGlobalCount = (nextSegment == null) ? 0 : nextSegment.GlobalCount;
            }

            public int LocalCount
            {
                get
                {
                    int lc = Volatile.Read(ref _localCount);
                    if (lc > SegmentSize)
                    {
                        return SegmentSize;
                    }
                    else
                    {
                        return lc;
                    }
                }
            }

            public Segment NextSegment
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return _nextSegment;
                }
            }

            public int GlobalCount
            {
                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                get
                {
                    return LocalCount + _nextSegmentGlobalCount;
                }
            }

            public T this[int index]
            {
                get
                {
                    if (index < 0 || _localCount <= index || SegmentSize <= index)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index ({index})");
                    }

                    return _data[index];
                }
            }

            internal bool TryAdd(T item)
            {
                int index = Interlocked.Increment(ref _localCount) - 1;
                if (index >= SegmentSize)
                {
                    Interlocked.Decrement(ref _localCount);
                    return false;
                }

                _data[index] = item;
                return true;
            }
        }
        #endregion class Segment
    }
}
