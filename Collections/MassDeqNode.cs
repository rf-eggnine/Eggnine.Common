// Copyright © 2025 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

namespace Eggnine.Common.Collections;

internal sealed class MassDeqNode<T>
{
    public T Value;
    public MassDeqNode<T>? Next;
    public MassDeqNode<T>? Prev;
    public MassDeqNode(T value) { Value = value; }
}