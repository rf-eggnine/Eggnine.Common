// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Collections.Generic;

namespace Eggnine.Common.Collections;

/// <summary>
/// Members <see cref="MassDeq{T}"/> offers beyond the standard <see cref="ICollection{T}"/> surface:
/// head-side insertion, contiguous mass-dequeue, predicate-based splicing, cloning, and a read-only
/// view.
/// </summary>
public interface IMassDeq<T> : ICollection<T>
{
    /// <summary>True if the deque is currently walking head-to-tail in reverse (see <see cref="Reverse"/>).</summary>
    bool IsReversed { get; }

    /// <summary>Flips which end Enqueue/TryDequeue/enumeration treat as head. O(1).</summary>
    void Reverse();

    /// <summary>Prepend at head. O(1).</summary>
    void Enqueue(T item);

    /// <summary>Removes a single item from the tail in O(1) time. Returns false if the deque is empty.</summary>
    bool TryDequeue(out T item);

    /// <summary>
    /// Detaches a contiguous run starting from the tail (or head, if <see cref="IsReversed"/>) for as
    /// long as <paramref name="predicate"/> holds, returning it as a new deque. Returns false, leaving
    /// this deque untouched, if empty or the boundary item doesn't match.
    /// </summary>
    bool TryMassDequeue(Func<T, bool> predicate, out IMassDeq<T> segment);

    /// <summary>
    /// Finds the first item matching <paramref name="predicate"/> and inserts <paramref name="item"/>
    /// immediately before it, atomically. Returns false if nothing matched.
    /// </summary>
    bool InsertBefore(T item, Func<T, bool> predicate);

    /// <summary>Removes the first item equal to <paramref name="item"/>, returning it via the out-style tuple.</summary>
    (bool Success, T? Value) TryRemove(T item);

    /// <summary>Copies matching items (or all, if <paramref name="wherePredicate"/> is null) into a new deque in the same order.</summary>
    IMassDeq<T> Clone(Func<T, bool>? wherePredicate = null);

    /// <summary>Same as <see cref="Clone"/> but the copy comes out reversed.</summary>
    IMassDeq<T> CloneReverse(Func<T, bool>? wherePredicate = null);

    /// <summary>Wraps this deque in a live, read-only <see cref="IReadOnlyCollection{T}"/> view.</summary>
    IReadOnlyCollection<T> AsReadOnly();
}
