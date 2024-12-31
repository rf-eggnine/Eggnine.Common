//  ©️ 2024 by RF At EggNine All Rights Reserved

using System;
using System.Collections.Generic;
using System.Threading;

namespace Eggnine.Common;

public class WhereableAsyncEnumerable<T> : IAsyncEnumerable<T>
{
    private IAsyncEnumerable<T> _enumerable;
    private Func<T, bool> _query;
    public WhereableAsyncEnumerable(IAsyncEnumerable<T> enumerable, Func<T, bool> query)
    {
        _enumerable = enumerable;
        _query = query;
    }
    public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        return new WhereableAsyncEnumerator<T>(_enumerable.GetAsyncEnumerator(), _query);
    }
}
