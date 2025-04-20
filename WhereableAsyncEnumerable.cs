// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

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
