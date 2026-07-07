// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Eggnine.Common.Collections;

/// <summary>
/// Doubly-linked deque with O(1) Enqueue (head) and O(1) TryMassDequeue: enumerates until
/// predicate(current.Value) is false, then splits. <c>foreach</c> (via the public
/// <see cref="GetEnumerator"/>) takes a detached snapshot under one lock acquisition, so it is
/// safe to enumerate concurrently with mutation from other threads: O(n) with one lock hold and
/// one copy. The class's own splicing operations (<see cref="Contains"/>, <see cref="TryRemove"/>,
/// <see cref="InsertBefore"/>, <see cref="CloneReverse"/>) walk the live list directly under the
/// same lock instead, avoiding that copy since they never release the lock mid-walk.
/// </summary>
public sealed class MassDeq<T> : IMassDeq<T>
{

    public MassDeq()
    {

    }
    private MassDeq(MassDeqNode<T> head, MassDeqNode<T> tail, int count)
    {
        _head = head;
        _tail = tail;
        _count = count;
    }

    internal readonly object _gate = new object();
    internal MassDeqNode<T>? _head;
    internal MassDeqNode<T>? _tail;
    internal int _count;
    internal bool _isReversed;
    public int Count => Volatile.Read(ref _count);

    public bool IsReversed => _isReversed;

    public bool IsReadOnly => false;

    public void Reverse()
    {
        lock (_gate)
        {
            _isReversed = !_isReversed;
        }
    }

    /// <summary>Prepend at head. O(1).</summary>
    public void Enqueue(T item)
    {
        MassDeqNode<T> node = new(item);
        lock (_gate)
        {
            if (_head is null || _tail is null)
            {
                _head = node;
                _tail = node;
            }
            else
            {
                if (_isReversed)
                {
                    node.Prev = _tail;
                    _tail.Next = node;
                    _tail = node;
                }
                else
                {
                    node.Next = _head;
                    _head.Prev = node;
                    _head = node;
                }
            }
            _count++;
        }
    }

    /// <summary>
    /// Detach until predicate returns false or entire list.
    /// **ONLY RETURNS CONTIGUOUS SEGMENTS**
    /// If empty, returns false.
    /// </summary>
    public bool TryMassDequeue(Func<T, bool> predicate, out MassDeq<T> segment)
    {
        MassDeqNode<T>? current;
        segment = new();
        lock (_gate)
        {
            if (_head is null || _tail is null)
            {
                return false;
            }
            if (!predicate(_isReversed ? _head.Value : _tail.Value))
            {
                return false;
            }
            current = _isReversed ? _head : _tail;
            MassDeqNode<T> headTail = current;
            for (int newCount = 1; current is not null; newCount++)
            {
                MassDeqNode<T>? nextPrev = _isReversed ? current.Next : current.Prev;
                if (nextPrev is null)
                {
                    segment = new(current, headTail, _count)
                    {
                        _isReversed = _isReversed
                    };
                    _head = null;
                    _tail = null;
                    _count = 0;
                    return true;
                }
                if (predicate(nextPrev.Value))
                {
                    current = nextPrev;
                }
                else
                {
                    segment = new(current, headTail, newCount);
                    if (_isReversed)
                    {
                        nextPrev.Prev = null;
                        _head = current.Next;
                        current.Next = null;
                    }
                    else
                    {
                        nextPrev.Next = null;
                        _tail = current.Prev;
                        current.Prev = null;
                    }
                    _count -= newCount;
                    return true;
                }
            }
        }
        throw new InvalidOperationException("This code should not be reachable");
    }

    /// <summary>Finds the first item matching <paramref name="predicate"/> and inserts
    /// <paramref name="item"/> immediately before it, atomically — the scan and the splice both
    /// happen under one lock hold, so a concurrent TryMassDequeue/TryRemove/etc. can't detach the
    /// found node between "found it" and "spliced next to it" (which would silently orphan the
    /// insert onto a detached segment the live deque no longer points to).</summary>
    public bool InsertBefore(T item, Func<T, bool> predicate)
    {
        lock (_gate)
        {
            for (MassDeqEnumerator<T> enumerator = GetLiveEnumerator();
                enumerator.MoveNext();)
            {
                if (predicate(enumerator.Current))
                {
                    enumerator.InsertBefore(item);
                    return true;
                }
            }
            return false;
        }
    }

    public MassDeq<T> Clone(Func<T, bool>? wherePredicate = null)
    {
        MassDeq<T> toReturn = CloneReverse(wherePredicate);
        toReturn.Reverse();
        return toReturn;
    }
    
    public MassDeq<T> CloneReverse(Func<T, bool>? wherePredicate = null)
    {
        MassDeq<T> toReturn = new();
        lock (_gate)
        {
            for (MassDeqEnumerator<T> enumerator = GetLiveEnumerator(); enumerator.MoveNext();)
            {
                T value = enumerator.Current;
                if (wherePredicate == null ? true : wherePredicate(value))
                {
                    toReturn.Enqueue(value);
                }
            }
        }
        return toReturn;
    }

    public IReadOnlyCollection<T> AsReadOnly()
    {
        return new ReadOnlyView<T>(this);
    }

    /// <summary>
    /// Removes a single item from the tail in O(1) time.
    /// Returns false if the deque is empty.
    /// </summary>
    public bool TryDequeue(out T item)
    {
        lock (_gate)
        {
            if (_tail is null || _head is null)
            {
                item = default!;
                return false;
            }

            MassDeqNode<T> node = _isReversed ? _head : _tail;
            item = node.Value;

            if ((_isReversed ? node.Next : node.Prev) is null)
            {
                // Only one element in the deque.
                _head = null;
                _tail = null;
            }
            else
            {
                // Detach the head or tail node.
                if (_isReversed)
                {
                    _head = node.Next;
                    _head!.Prev = null;
                    node.Next = null;
                }
                else
                {
                    _tail = node.Prev;
                    _tail!.Next = null;
                    node.Prev = null;
                }
            }
            _count--;
            return true;
        }
    }


    /// <summary>
    /// Returns a detached, immutable snapshot enumerator, what <c>foreach</c> binds to. Safe to
    /// walk concurrently with mutation on the live deque: the snapshot is copied under one
    /// <see cref="_gate"/> acquisition before this method returns, so nothing the caller does
    /// afterward can observe a concurrent Enqueue/TryDequeue/TryRemove. Calling
    /// <see cref="MassDeqEnumerator{T}.InsertBefore"/> or <see cref="MassDeqEnumerator{T}.Remove"/>
    /// on the result throws, since there is no live list underneath it to splice into.
    /// </summary>
    public MassDeqEnumerator<T> GetEnumerator()
    {
        lock (_gate)
        {
            MassDeqNode<T>? snapshotHead = null;
            MassDeqNode<T>? snapshotTail = null;
            MassDeqNode<T>? source = _isReversed ? _tail : _head;
            while (source is not null)
            {
                MassDeqNode<T> copy = new(source.Value);
                if (snapshotTail is null)
                {
                    snapshotHead = copy;
                }
                else
                {
                    snapshotTail.Next = copy;
                    copy.Prev = snapshotTail;
                }
                snapshotTail = copy;
                source = _isReversed ? source.Prev : source.Next;
            }
            return new MassDeqEnumerator<T>(this, snapshotHead, isReversed: false, isSnapshot: true);
        }
    }

    /// <summary>
    /// Returns an enumerator over the live list, for the class's own splicing operations
    /// (<see cref="Contains"/>, <see cref="TryRemove"/>, <see cref="InsertBefore"/>,
    /// <see cref="CloneReverse"/>) to walk under a lock they already hold, without paying for a
    /// snapshot copy they'd immediately discard. Not safe to use outside a <see cref="_gate"/>
    /// hold, hence internal rather than public.
    /// </summary>
    internal MassDeqEnumerator<T> GetLiveEnumerator()
    {
        return new MassDeqEnumerator<T>(this, _isReversed ? _tail : _head, _isReversed);
    }

    IEnumerator<T> IEnumerable<T>.GetEnumerator()
    {
        return GetEnumerator();
    }
    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(T item)
    {
        Enqueue(item);
    }

    public void Clear()
    {
        lock (_gate)
        {
            _head = null;
            _tail = null;
            _count = 0;
            _isReversed = false;
        }
        return;
    }

    public bool Contains(T item)
    {
        lock (_gate)
        {
            for (MassDeqEnumerator<T> enumerator = GetLiveEnumerator(); enumerator.MoveNext();)
            {
                T itemInside = enumerator.Current;
                if (itemInside is null)
                {
                    if (item is null)
                    {
                        return true;
                    }
                }
                else if (itemInside.Equals(item))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        MassDeq<T> reverseClone;
        lock (_gate)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }
            if (arrayIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(arrayIndex), "arrayIndex must be non-negative.");
            }
            if (array.Length - arrayIndex < Count)
            {
                throw new ArgumentException("The number of elements in the source MassDeq is greater than the available space from arrayIndex to the end of the destination array.");
            }
            reverseClone = CloneReverse();
        }
        T t = default!;
        while (arrayIndex < array.Length && reverseClone.TryDequeue(out t))
        {
            array[arrayIndex++] = t;
        }
    }

    bool ICollection<T>.Remove(T item)
    {
        var (success, value) = TryRemove(item);
        return success;
    }

    public (bool Success, T? Value) TryRemove(T item)
    {
        lock (_gate)
        {
            for (MassDeqEnumerator<T> enumerator = GetLiveEnumerator();
                enumerator.MoveNext();)
            {
                if (Equals(enumerator.Current, item))
                {
                    return (true, enumerator.Remove());
                }
            }
            return (false, default);
        }
    }

    bool IMassDeq<T>.TryMassDequeue(Func<T, bool> predicate, out IMassDeq<T> segment)
    {
        bool result = TryMassDequeue(predicate, out MassDeq<T> concreteSegment);
        segment = concreteSegment;
        return result;
    }

    IMassDeq<T> IMassDeq<T>.Clone(Func<T, bool>? wherePredicate) => Clone(wherePredicate);

    IMassDeq<T> IMassDeq<T>.CloneReverse(Func<T, bool>? wherePredicate) => CloneReverse(wherePredicate);
}
