// Copyright © 2025 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System.Collections.Generic;

namespace Eggnine.Common;

/// <summary>
/// Generates a lazy, inclusive sequence of consecutive <see cref="long"/> values from
/// <paramref name="start"/> to <paramref name="end"/>. Fills the gap left by
/// <see cref="System.Linq.Enumerable.Range(int, int)"/>, which only accepts <see cref="int"/>.
/// </summary>
public static class LongRange
{
    public static IEnumerable<long> Of(long start, long end)
    {
        for (long i = start; i <= end; i++)
            yield return i;
    }
}
