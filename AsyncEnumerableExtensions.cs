// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eggnine.Common;

public static class AsyncEnumerableExtensions
{
    public static WhereableAsyncEnumerable<T> WhereAsync<T>(this IAsyncEnumerable<T> enumerable, Func<T,bool> query)
    {
        return new WhereableAsyncEnumerable<T>(enumerable, query);
    }

    public static async Task<T> FirstAsync<T>(this IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default)
    {
        await using IAsyncEnumerator<T> enumerator = enumerable.GetAsyncEnumerator(cancellationToken);
        if (!await enumerator.MoveNextAsync())
        {
            throw new InvalidOperationException("Sequence contains no elements.");
        }
        return enumerator.Current;
    }
}
