// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Eggnine.Common.Collections;

/// <summary>
/// Doubly-linked deque with O(1) operations at either end and O(1) TryMassDequeue: enumerates
/// until predicate(current.Value) is false, then splits. Every member names the physical end it
/// operates on explicitly (<see cref="EnqueueHead"/>/<see cref="EnqueueTail"/>,
/// <see cref="TryDequeueHead"/>/<see cref="TryDequeueTail"/>, <see cref="TryPeekHead"/>/
/// <see cref="TryPeekTail"/>) rather than relying on a BCL-Queue-style implicit convention —
/// nothing here is a "front" or "back" you have to remember, it's always literally the head or
/// the tail. Enumeration (via <c>foreach</c>/<see cref="GetEnumerator"/>) always walks head to
/// tail; <see cref="GetReversedEnumerator"/> walks the other direction for callers that
/// specifically need that, without any mutable per-instance "reversed" state to reason about.
/// <c>foreach</c>/<see cref="GetEnumerator"/>/<see cref="GetReversedEnumerator"/> all take a
/// detached snapshot under one lock acquisition, so it is safe to enumerate concurrently with
/// mutation from other threads: O(n) with one lock hold and one copy. The class's own splicing
/// operations (<see cref="Contains"/>, <see cref="TryRemove"/>, <see cref="InsertBefore"/>,
/// <see cref="CloneReverse"/>) walk the live list directly under the same lock instead, avoiding
/// that copy since they never release the lock mid-walk.
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
    public int Count => Volatile.Read(ref _count);

    public bool IsReadOnly => false;

    /// <summary>Insert at the head (front). O(1).</summary>
    public void EnqueueHead(T item)
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
                node.Next = _head;
                _head.Prev = node;
                _head = node;
            }
            _count++;
        }
    }

    /// <summary>Insert at the tail (back). O(1).</summary>
    public void EnqueueTail(T item)
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
                node.Prev = _tail;
                _tail.Next = node;
                _tail = node;
            }
            _count++;
        }
    }

    /// <summary>
    /// Detach a contiguous run starting from the head, for as long as
    /// <paramref name="predicate"/> holds, returning it as a new deque.
    /// **ONLY RETURNS CONTIGUOUS SEGMENTS**
    /// If empty, returns false.
    /// </summary>
    public bool TryMassDequeue(Predicate<T> predicate, out MassDeq<T> segment)
    {
        MassDeqNode<T>? current;
        segment = new();
        lock (_gate)
        {
            if (_head is null || _tail is null)
            {
                return false;
            }
            if (!predicate(_head.Value))
            {
                return false;
            }
            current = _head;
            MassDeqNode<T> originalHead = current;
            for (int newCount = 1; current is not null; newCount++)
            {
                MassDeqNode<T>? next = current.Next;
                if (next is null)
                {
                    // Entire list matched.
                    segment = new(originalHead, current, _count);
                    _head = null;
                    _tail = null;
                    _count = 0;
                    return true;
                }
                if (predicate(next.Value))
                {
                    current = next;
                }
                else
                {
                    // segment spans [originalHead .. current] — head-to-tail order preserved,
                    // matching this deque's own head-is-front convention.
                    segment = new(originalHead, current, newCount);
                    next.Prev = null;
                    _head = next;
                    current.Next = null;
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
    public bool InsertBefore(T item, Predicate<T> predicate)
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

    /// <summary>Finds the first item matching <paramref name="predicate"/> and inserts
    /// <paramref name="item"/> immediately after it, atomically — the scan and the splice both
    /// happen under one lock hold, so a concurrent TryMassDequeue/TryRemove/etc. can't detach the
    /// found node between "found it" and "spliced next to it" (which would silently orphan the
    /// insert onto a detached segment the live deque no longer points to).</summary>
    public bool InsertAfter(T item, Predicate<T> predicate)
    {
        lock (_gate)
        {
            for (MassDeqEnumerator<T> enumerator = GetLiveEnumerator();
                enumerator.MoveNext();)
            {
                if (predicate(enumerator.Current))
                {
                    enumerator.InsertAfter(item);
                    return true;
                }
            }
            return false;
        }
    }

    /// <summary>Faithful copy — same head-to-tail order as this deque.</summary>
    public MassDeq<T> Clone(Predicate<T>? wherePredicate = null)
    {
        MassDeq<T> toReturn = new();
        lock (_gate)
        {
            for (MassDeqEnumerator<T> enumerator = GetLiveEnumerator(); enumerator.MoveNext();)
            {
                T value = enumerator.Current;
                if (wherePredicate == null || wherePredicate(value))
                {
                    toReturn.EnqueueTail(value);
                }
            }
        }
        return toReturn;
    }

    /// <summary>Same as <see cref="Clone"/> but the copy comes out in reverse (tail-to-head)
    /// order — walks this deque head-to-tail once, prepending each match, so the last item
    /// visited (this deque's own tail) ends up at the clone's head.</summary>
    public MassDeq<T> CloneReverse(Predicate<T>? wherePredicate = null)
    {
        MassDeq<T> toReturn = new();
        lock (_gate)
        {
            for (MassDeqEnumerator<T> enumerator = GetLiveEnumerator(); enumerator.MoveNext();)
            {
                T value = enumerator.Current;
                if (wherePredicate == null || wherePredicate(value))
                {
                    toReturn.EnqueueHead(value);
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
    /// Removes a single item from the head (front) in O(1) time.
    /// Returns false if the deque is empty.
    /// </summary>
    public bool TryDequeueHead(out T item)
    {
        lock (_gate)
        {
            if (_tail is null || _head is null)
            {
                item = default!;
                return false;
            }

            MassDeqNode<T> node = _head;
            item = node.Value;

            if (node.Next is null)
            {
                // Only one element in the deque.
                _head = null;
                _tail = null;
            }
            else
            {
                _head = node.Next;
                _head.Prev = null;
                node.Next = null;
            }
            _count--;
            return true;
        }
    }

    /// <summary>
    /// Removes a single item from the tail (back) in O(1) time.
    /// Returns false if the deque is empty.
    /// </summary>
    public bool TryDequeueTail(out T item)
    {
        lock (_gate)
        {
            if (_tail is null || _head is null)
            {
                item = default!;
                return false;
            }

            MassDeqNode<T> node = _tail;
            item = node.Value;

            if (node.Prev is null)
            {
                // Only one element in the deque.
                _head = null;
                _tail = null;
            }
            else
            {
                _tail = node.Prev;
                _tail.Next = null;
                node.Prev = null;
            }
            _count--;
            return true;
        }
    }


    /// <summary>
    /// Returns a detached, immutable snapshot enumerator, what <c>foreach</c> binds to. Walks
    /// head to tail. Safe to walk concurrently with mutation on the live deque: the snapshot is
    /// copied under one <see cref="_gate"/> acquisition before this method returns, so nothing
    /// the caller does afterward can observe a concurrent EnqueueHead/EnqueueTail/TryDequeueHead/
    /// TryDequeueTail/TryRemove. Calling <see cref="MassDeqEnumerator{T}.InsertBefore"/> or
    /// <see cref="MassDeqEnumerator{T}.Remove"/> on the result throws, since there is no live list
    /// underneath it to splice into.
    /// </summary>
    public MassDeqEnumerator<T> GetEnumerator()
    {
        lock (_gate)
        {
            MassDeqNode<T>? snapshotHead = null;
            MassDeqNode<T>? snapshotTail = null;
            MassDeqNode<T>? source = _head;
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
                source = source.Next;
            }
            return new MassDeqEnumerator<T>(this, snapshotHead, isReversed: false, isSnapshot: true);
        }
    }

    /// <summary>
    /// Same as <see cref="GetEnumerator"/> but walks tail to head instead — for callers with a
    /// specific need to consume the deque back-to-front without physically reversing it or
    /// paying for a <see cref="CloneReverse"/>. A detached snapshot, same safety guarantees as
    /// <see cref="GetEnumerator"/>.
    /// </summary>
    public MassDeqEnumerator<T> GetReversedEnumerator()
    {
        lock (_gate)
        {
            MassDeqNode<T>? snapshotHead = null;
            MassDeqNode<T>? snapshotTail = null;
            MassDeqNode<T>? source = _tail;
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
                source = source.Prev;
            }
            return new MassDeqEnumerator<T>(this, snapshotHead, isReversed: false, isSnapshot: true);
        }
    }

    /// <summary>
    /// Returns an enumerator over the live list, for the class's own splicing operations
    /// (<see cref="Contains"/>, <see cref="TryRemove"/>, <see cref="InsertBefore"/>,
    /// <see cref="Clone"/>, <see cref="CloneReverse"/>) to walk under a lock they already hold,
    /// without paying for a snapshot copy they'd immediately discard. Always walks head to tail —
    /// not safe to use outside a <see cref="_gate"/> hold, hence internal rather than public.
    /// </summary>
    internal MassDeqEnumerator<T> GetLiveEnumerator()
    {
        return new MassDeqEnumerator<T>(this, _head, isReversed: false);
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
        EnqueueTail(item);
    }

    public void Clear()
    {
        lock (_gate)
        {
            _head = null;
            _tail = null;
            _count = 0;
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
        MassDeq<T> clone;
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
            clone = Clone();
        }
        T t = default!;
        while (arrayIndex < array.Length && clone.TryDequeueHead(out t))
        {
            array[arrayIndex++] = t;
        }
    }

    bool ICollection<T>.Remove(T item) => TryRemove(item, out _);

    /// <summary>Removes the first item equal to <paramref name="item"/>, in head-to-tail
    /// (enumeration) order. Returns false, leaving <paramref name="removed"/> default, if nothing
    /// matched.</summary>
    public bool TryRemove(T item, out T? removed)
    {
        lock (_gate)
        {
            for (MassDeqEnumerator<T> enumerator = GetLiveEnumerator();
                enumerator.MoveNext();)
            {
                if (Equals(enumerator.Current, item))
                {
                    removed = enumerator.Remove();
                    return true;
                }
            }
            removed = default;
            return false;
        }
    }

    /// <summary>Looks at the item <see cref="DequeueHead"/>/<see cref="TryDequeueHead"/> would
    /// return, without removing it. Returns false if the deque is empty.</summary>
    public bool TryPeekHead(out T item)
    {
        lock (_gate)
        {
            MassDeqNode<T>? node = _head;
            if (node is null)
            {
                item = default!;
                return false;
            }
            item = node.Value;
            return true;
        }
    }

    /// <summary>Looks at the item <see cref="DequeueTail"/>/<see cref="TryDequeueTail"/> would
    /// return, without removing it. Returns false if the deque is empty.</summary>
    public bool TryPeekTail(out T item)
    {
        lock (_gate)
        {
            MassDeqNode<T>? node = _tail;
            if (node is null)
            {
                item = default!;
                return false;
            }
            item = node.Value;
            return true;
        }
    }

    /// <summary>Same as <see cref="TryPeekHead"/> but throws <see cref="InvalidOperationException"/>
    /// instead of returning false when the deque is empty — matches <see cref="Queue{T}.Peek"/>.</summary>
    public T PeekHead()
    {
        if (!TryPeekHead(out T item))
        {
            throw new InvalidOperationException("MassDeq is empty.");
        }
        return item;
    }

    /// <summary>Same as <see cref="TryPeekTail"/> but throws <see cref="InvalidOperationException"/>
    /// instead of returning false when the deque is empty.</summary>
    public T PeekTail()
    {
        if (!TryPeekTail(out T item))
        {
            throw new InvalidOperationException("MassDeq is empty.");
        }
        return item;
    }

    /// <summary>Same as <see cref="TryDequeueHead"/> but throws <see cref="InvalidOperationException"/>
    /// instead of returning false when the deque is empty — matches <see cref="Queue{T}.Dequeue"/>.</summary>
    public T DequeueHead()
    {
        if (!TryDequeueHead(out T item))
        {
            throw new InvalidOperationException("MassDeq is empty.");
        }
        return item;
    }

    /// <summary>Same as <see cref="TryDequeueTail"/> but throws <see cref="InvalidOperationException"/>
    /// instead of returning false when the deque is empty.</summary>
    public T DequeueTail()
    {
        if (!TryDequeueTail(out T item))
        {
            throw new InvalidOperationException("MassDeq is empty.");
        }
        return item;
    }

    bool IMassDeq<T>.TryMassDequeue(Predicate<T> predicate, out IMassDeq<T> segment)
    {
        bool result = TryMassDequeue(predicate, out MassDeq<T> concreteSegment);
        segment = concreteSegment;
        return result;
    }

    IMassDeq<T> IMassDeq<T>.Clone(Predicate<T>? wherePredicate) => Clone(wherePredicate);

    IMassDeq<T> IMassDeq<T>.CloneReverse(Predicate<T>? wherePredicate) => CloneReverse(wherePredicate);
}
