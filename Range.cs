using System;
using System.Collections;
using System.Collections.Generic;

namespace Eggnine.Common;

public class Range : IEnumerable<long>
{
    private readonly long _start;
    private readonly long _end;
    public Range(long start, long end)
    {
        _start = start;
        _end = end;
    }

    public IEnumerator<long> GetEnumerator()
    {
        return new RangeEnumerator(this);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public class RangeEnumerator : IEnumerator<long>
    {
        private bool _disposed;
        private readonly Range _range;
        private long? _index;
        public RangeEnumerator(Range range)
        {
            _range = range;
            _index = null;
        }

        public long Current => CheckForDisposedAndReturnCurrent();

        object IEnumerator.Current => Current;

        public void Dispose()
        {
            if(!_disposed)
            {
                _disposed = true;
                _index = null;
            }            
        }

        public bool MoveNext()
        {
            if(_index is null)
            {
                _index = _range._start;
            }
            else
            {
                _index++;
            }
            if(_index > _range._end)
            {
                return false;
            }
            return true;
        }

        public void Reset()
        {
            _index = null;
        }

        private long CheckForDisposedAndReturnCurrent()
        {
            if(_disposed)
            {
                throw new ObjectDisposedException(nameof(Range));
            }
            return _index ?? long.MinValue;
        }
    }
}
