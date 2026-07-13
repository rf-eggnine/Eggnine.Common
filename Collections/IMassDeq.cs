// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Collections.Generic;

namespace Eggnine.Common.Collections;

/// <summary>
/// Members <see cref="MassDeq{T}"/> offers beyond the standard <see cref="ICollection{T}"/> surface:
/// tail-side insertion, contiguous mass-dequeue, predicate-based splicing, and cloning (including
/// frozen clones via <c>asFrozen</c>). Also declares <see cref="IReadOnlyCollection{T}"/> since
/// <see cref="MassDeq{T}"/> already satisfies it structurally (via <see cref="ICollection{T}.Count"/>
/// and its enumerator) — callers that only need read access can accept <see cref="IMassDeq{T}"/>
/// directly.
/// </summary>
public interface IMassDeq<T> : ICollection<T>, IReadOnlyCollection<T>
{
    /// <summary>
    /// Insert at the head (front). O(1). Throws <see cref="InvalidOperationException"/> if frozen.
    /// </summary>
    /// <param name="item"></param>
    void EnqueueHead(T item);

    /// <summary>
    /// Insert at the tail (back). O(1). Throws <see cref="InvalidOperationException"/> if frozen.
    /// It matches <see cref="Queue{T}.Queue"/>
    /// </summary>
    /// <param name="item"></param>
    void EnqueueTail(T item);

    /// <summary>
    /// Removes a single item from the head (front) in O(1) time. Returns false if the deque is empty or frozen.
    /// </summary>
    /// <param name="item"></param>
    /// <returns>false when the deque is empty or frozen</returns>
    bool TryDequeueHead(out T item);

    /// <summary>
    /// Same as <see cref="TryDequeueHead"/> but throws <see cref="InvalidOperationException"/>
    /// instead of returning false when the deque is empty or frozen. It matches <see cref="Queue{T}.Dequeue"/>.
    /// </summary>
    /// <returns>the head</returns>
    T DequeueHead();

    /// <summary>
    /// Removes a single item from the tail (back) in O(1) time. Returns false if the deque is empty or frozen.
    /// </summary>
    /// <param name="item"></param>
    /// <returns>false when the deque is empty or frozen</returns>
    bool TryDequeueTail(out T item);

    /// <summary>
    /// Same as <see cref="TryDequeueTail"/> but throws <see cref="InvalidOperationException"/>
    /// instead of returning false when the deque is empty or frozen
    /// </summary>
    /// <returns>the tail</returns>
    T DequeueTail();

    /// <summary>
    /// Looks at the item <see cref="DequeueHead"/>/<see cref="TryDequeueHead"/> would return,
    /// without removing it. Returns false if the deque is empty.
    /// </summary>
    /// <param name="item"></param>
    /// <returns>false if the deque is empty.</returns>
    bool TryPeekHead(out T item);

    /// <summary>
    /// Same as <see cref="TryPeekHead"/> but throws <see cref="InvalidOperationException"/>
    /// instead of returning false when the deque is empty — matches <see cref="Queue{T}.Peek"/>.
    /// </summary>
    /// <returns>the head</returns>
    T PeekHead();

    /// <summary>
    /// Looks at the item <see cref="DequeueTail"/>/<see cref="TryDequeueTail"/> would return,
    /// without removing it. Returns false if the deque is empty.
    /// </summary>
    /// <returns>false if the deque is empty.</returns>
    bool TryPeekTail(out T item);

    /// <summary>
    /// Same as <see cref="TryPeekTail"/> but throws <see cref="InvalidOperationException"/>
    /// instead of returning false when the deque is empty.
    /// </summary>
    /// <returns>the tail</returns>
    T PeekTail();

    /// <summary>
    /// Detaches a contiguous run starting from the head, for as long as <paramref name="predicate"/>
    /// holds, returning it as a new deque. Returns false, leaving this deque untouched, if empty or frozen
    /// or the boundary item doesn't match.
    /// </summary>
    /// <param name="predicate"></param>
    /// <param name="segment"></param>
    /// <returns></returns>
    bool TryMassDequeue(Predicate<T> predicate, out IMassDeq<T> segment);

    /// <summary>
    /// Finds the first item matching <paramref name="predicate"/> and inserts <paramref name="item"/>
    /// immediately before it, atomically. Returns false if nothing matched.
    /// Throws <see cref="InvalidOperationException"/> if frozen.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    bool InsertBefore(T item, Predicate<T> predicate);

    /// <summary>
    /// Finds the first item matching <paramref name="predicate"/> and inserts <paramref name="item"/>
    /// immediately after it, atomically. Returns false if nothing matched.
    /// Throws <see cref="InvalidOperationException"/> if frozen.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="predicate"></param>
    /// <returns></returns>
    bool InsertAfter(T item, Predicate<T> predicate);

    /// <summary>
    /// Removes the first item equal to <paramref name="item"/>. Returns false, leaving
    /// <paramref name="removed"/> default, if nothing matched.
    /// Throws <see cref="InvalidOperationException"/> if frozen.
    /// </summary>
    /// <param name="item"></param>
    /// <param name="removed"></param>
    /// <returns></returns>
    bool TryRemove(T item, out T? removed);

    /// <summary>
    /// Copies matching items (or all, if <paramref name="wherePredicate"/> is null) into a new deque in the same order.
    /// </summary>
    /// <param name="wherePredicate"></param>
    /// <param name="asFrozen"></param>
    /// <returns></returns>
    IMassDeq<T> Clone(Predicate<T>? wherePredicate = null, bool asFrozen = false);

    /// <summary>
    /// Same as <see cref="Clone"/> but the copy comes out reversed.
    /// </summary>
    /// <param name="wherePredicate"></param>
    /// <param name="asFrozen"></param>
    /// <returns></returns>
    IMassDeq<T> CloneReverse(Predicate<T>? wherePredicate = null, bool asFrozen = false);
}
