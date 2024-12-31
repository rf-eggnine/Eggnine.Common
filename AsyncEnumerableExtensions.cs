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
        IAsyncEnumerator<T> enumerator = enumerable.GetAsyncEnumerator();
        await Task.Run(async() => await enumerator.MoveNextAsync(), cancellationToken);
        return enumerator.Current;
    }
}
