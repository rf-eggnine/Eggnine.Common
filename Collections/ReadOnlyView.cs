// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Collections;
using System.Collections.Generic;

namespace Eggnine.Common.Collections;

public class ReadOnlyView<T> : IReadOnlyCollection<T>
{
    readonly ICollection<T> _collection;

    public ReadOnlyView(ICollection<T> collection)
    {
        _collection = collection;
    }
    public int Count => _collection.Count;

    public IEnumerator<T> GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}