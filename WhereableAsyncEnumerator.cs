// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Eggnine.Common;

public class WhereableAsyncEnumerator<T> : IAsyncEnumerator<T>, IAsyncDisposable
{
    private SemaphoreSlim _semaphore = new(1,1);
    private bool _disposed = false;
    private IAsyncEnumerator<T> _enumerator;
    private Func<T, bool> _query;
    private CancellationToken _cancellationToken;
    public WhereableAsyncEnumerator(IAsyncEnumerator<T> enumerator, Func<T, bool> query, CancellationToken cancellationToken = default)
    {
        _enumerator = enumerator;
        _query = query;
        _cancellationToken = cancellationToken;
    }

    public T Current
    {
        get
        {
            CheckDisposed();
            _semaphore.WaitAsync(_cancellationToken).GetAwaiter().GetResult();
            try
            {
                return _enumerator.Current;
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if(_enumerator is null || _disposed)
        {
            return;
        }
        _disposed = true;
        await _enumerator.DisposeAsync();
    }

    public async ValueTask<bool> MoveNextAsync()
    {
        CheckDisposed();
        await _semaphore.WaitAsync();
        try
        {
            if(!await _enumerator.MoveNextAsync())
            {
                return false;
            }
            while(!_query(_enumerator.Current))
            {
                if(!await _enumerator.MoveNextAsync())
                {
                    return false;
                }
            }
            return true;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private void CheckDisposed()
    {
        if(_disposed)
        {
            throw new ObjectDisposedException(nameof(WhereableAsyncEnumerator<T>));
        }
    }
}
