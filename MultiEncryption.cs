// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Eggnine.Common;

internal sealed class MultiEncryption : IMultiEncryption, IDisposable
{
    private readonly IReadOnlyList<IEncryptionV2> _algorithms;
    private readonly IEncryptionV2 _preferred; // used for Encrypt()
    private bool _disposed;

    /// <param name="algorithms">
    /// Ordered list of modern algorithms (most preferred first).
    /// Example: new IEncryptionV2[] { new Encryption2Peppered(ring), new Encryption2() }
    /// </param>
    /// <param name="preferred">
    /// If null, the first element of <paramref name="algorithms"/> is used for Encrypt().
    /// </param>
    public MultiEncryption(IEnumerable<IEncryptionV2> algorithms,
                            IEncryptionV2? preferred = null)
    {
        _algorithms = (algorithms ?? throw new ArgumentNullException(nameof(algorithms))).ToArray();
        if (_algorithms.Count == 0)
            throw new ArgumentException("At least one algorithm must be provided.", nameof(algorithms));

        _preferred = preferred ?? _algorithms[0];
    }

    /// <summary>
    /// Create a new hash using the preferred algorithm.
    /// </summary>
    public string Encrypt(string s)
    {
        CheckDisposed();
        return _preferred.Encrypt(s);
    }

    /// <summary>
    /// Verify against the provided stored hash.
    /// Tries algorithms in order where MatchesAlg(stored) == true.
    /// </summary>
    public bool VerifyEncryption(string input, string stored)
    {
        CheckDisposed();
        if (string.IsNullOrWhiteSpace(stored))
            return false;

        foreach (var alg in _algorithms)
        {
            if (alg.IsTargetFormat(stored))
                return alg.VerifyEncryption(input, stored);
        }

        return false;
    }

    /// <summary>
    /// Returns true if any contained algorithm recognizes the format.
    /// </summary>
    public bool IsTargetFormat(string input)
    {
        CheckDisposed();
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return _algorithms.Any(a => a.IsTargetFormat(input));
    }

    public async Task<(bool Verified, bool RequiresUpgrade)> VerifyAndCheckUpgradeAsync(
        string input,
        string? stored)
    {
        return await VerifyAndUpgradeAsync(input, stored, _ => Task.CompletedTask);
    }

    public async Task<(bool Verified, bool Upgraded)> VerifyAndUpgradeAsync(
        string input,
        string? stored,
        Func<string, Task> persistNewHashAsync)
    {
        if (string.IsNullOrWhiteSpace(stored))
            return (false, false);

        if (!VerifyEncryption(input, stored))
            return (false, false);

        if (_preferred.IsTargetFormat(stored))
            return (true, false);

        var upgraded = _preferred.Encrypt(input);
        if (!string.Equals(upgraded, stored, StringComparison.Ordinal))
        {
            await persistNewHashAsync(upgraded);
            return (true, true);
        }

        return (true, false);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var a in _algorithms)
            (a as IDisposable)?.Dispose();
    }

    private void CheckDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(MultiEncryption));
    }
}