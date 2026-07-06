// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Eggnine.Common.Collections;

/// <summary>
/// Walks a chain of nodes starting at <c>start</c>. Two distinct modes, depending on how it was
/// obtained:
/// <list type="bullet">
/// <item>From <see cref="MassDeq{T}.GetEnumerator"/> (what <c>foreach</c> uses): <c>isSnapshot</c>
/// is true, the chain is a detached, private copy nothing else can see or mutate.
/// <see cref="InsertBefore"/>/<see cref="Remove"/> throw on this mode, there is no live list
/// underneath to splice into.</item>
/// <item>From <see cref="MassDeq{T}.GetLiveEnumerator"/> (internal use only, by the class's own
/// splicing operations): walks the live, shared nodes directly, with no allocation and no
/// locking of its own, always called from inside a <c>lock (_gate)</c> the caller already holds.
/// Not safe to use outside that lock.</item>
/// </list>
/// </summary>
public struct MassDeqEnumerator<T> : IEnumerator<T>
{
    private MassDeq<T> _original;
    private MassDeqNode<T>? _start;
    private MassDeqNode<T>? _current;
    private MassDeqNode<T>? _next;
    private readonly bool _isReversed;
    private readonly bool _isSnapshot;

    internal MassDeqEnumerator(MassDeq<T> original, MassDeqNode<T>? start, bool isReversed, bool isSnapshot = false)
    {
        _original = original;
        _isReversed = isReversed;
        _isSnapshot = isSnapshot;
        _current = default;
        _next = start;
        _start = start;
    }

    public T Current
    {
        get
        {
            if (_current is null) return ThrowNotStartedOrEnded();
            else return _current.Value;
        }
    }

    object IEnumerator.Current
    {
        get
        {
            if (_current is null) return ThrowNotStartedOrEnded()!;
            else return _current.Value!;
        }
    }

    public bool MoveNext()
    {
        MassDeqNode<T>? n = _next;
        if (n is null) return false;

        _current = n;
        _next = _isReversed ? n.Prev : n.Next;
        return true;
    }

    public void InsertBefore(T item)
    {
        if (_isSnapshot)
            throw new InvalidOperationException("Cannot insert into a snapshot enumerator (from foreach/GetEnumerator) — it is detached from the live deque. Use MassDeq<T>.InsertBefore instead.");
        if (_current is null)
            throw new InvalidOperationException("Cannot insert: enumeration has ended or has not started.");

        lock (_original._gate)
        {
            MassDeqNode<T> node = new(item);

            if (_isReversed)
            {
                // In reverse: enumerate tail→head, so "before" means after in physical order
                node.Prev = _current;
                node.Next = _current.Next;
                if (_current.Next != null)
                    _current.Next.Prev = node;
                _current.Next = node;
                if (_current == _original._tail)
                    _original._tail = node;
            }
            else
            {
                // Normal: enumerate head→tail, insert before in physical order
                node.Next = _current;
                node.Prev = _current.Prev;
                if (_current.Prev != null)
                    _current.Prev.Next = node;
                _current.Prev = node;
                if (_current == _original._head)
                    _original._head = node;
            }
            _original._count++;
        }
    }

    public T? Remove()
    {
        if (_isSnapshot)
        {
            throw new InvalidOperationException("Cannot remove from a snapshot enumerator (from foreach/GetEnumerator) — it is detached from the live deque. Use MassDeq<T>.TryRemove instead.");
        }
        if (_current is null)
        {
            throw new InvalidOperationException("Cannont remove current before moveNext or after end of enumerable");
        }
        lock (_original._gate)
        {
            T? t = default;
            if (_original._head is not null)
            {
                t = _original._head.Value;
            }
            if (_isReversed)
            {
                if (_current.Next is null)
                {
                    _original._count--;
                    _original._head = null;
                    _original._tail = null;
                    _current = null;
                    _next = null;
                    if (t is not null)
                    {
                        return t;
                    }
                    else
                    {
                        return default;
                    }
                }
                if (_current.Prev is not null)
                {
                    _current.Prev.Next = _current.Next;
                    _current.Next.Prev = _current.Prev;
                }
                else
                {
                    _current.Next.Prev = null;
                }
                // Detach the removed node's own pointers so a concurrent reader that already
                // captured it as _current can't keep walking through it into stale/detached
                // territory (mirrors TryDequeue's existing discipline).
                _current.Next = null;
                _current.Prev = null;
                _original._count--;
            }
            else
            {
                if (_current.Prev is null)
                {
                    _original.Clear();
                    _current = null;
                    _next = null;
                    if (t is not null)
                    {
                        return t;
                    }
                    else
                    {
                        return default;
                    }
                }
                if (_current.Next is not null)
                {
                    _current.Next.Prev = _current.Prev;
                    _current.Prev.Next = _current.Next;
                }
                else
                {
                    _current.Prev.Next = null;
                }
                _current.Next = null;
                _current.Prev = null;
                _original._count--;
            }
            if (t is not null)
            {
                return t;
            }
            else
            {
                return default;
            }
        }
    }

    public void Reset()
    {
        _current = default;
        _next = _start;
    }

    public void Dispose()
    {
        _next = null;
        _current = default;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ThrowNotStartedOrEnded()
        => throw new InvalidOperationException("Enumeration not started or already finished.");
}