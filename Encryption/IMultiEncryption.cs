// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System;
using System.Threading.Tasks;

namespace Eggnine.Common;

public interface IMultiEncryption : IEncryptionV2
{
    /// <summary>
    /// Verify input against 'stored' using a flexible verifier (e.g., CompatEncryption).
    /// If verified and 'stored' is NOT in the target algorithm format, re-hash with 'target'
    /// and persist via callback. Returns true IFF verification succeeds.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="stored"></param>
    /// <returns>
    ///     (false, false) if the input did not verify against the stored
    ///     (true, false) if the input verified against the stored and the alg was the latest
    ///     (true, true) if the input verified against the stored and the stored requires an upgrade
    /// </returns>
    public Task<(bool Verified, bool RequiresUpgrade)> VerifyAndCheckUpgradeAsync(
        string input,
        string? stored);

    /// <summary>
    /// Similar to VerifyAndUpgradeAsync but returns both the verify result
    /// and the upgrade result
    /// </summary>
    /// <param name="input"></param>
    /// <param name="stored"></param>
    /// <param name="persistNewHashAsync"></param>
    /// <returns>
    ///     (false, false) if the input did not verify against the stored
    ///     (true, false) if the input verified against the stored and the alg was the latest
    ///     (true, true) if the input verified against the stored and the stored was upgraded
    /// </returns>
    public Task<(bool Verified, bool Upgraded)> VerifyAndUpgradeAsync(
        string input,
        string? stored,
        Func<string, Task> persistNewHashAsync);

    internal static IMultiEncryption GetMultiEncryption(EncryptionOptions options)
    {
        IEncryptionV2 secondary = new EncryptionV2(options.Iterations, options.SaltLength, options.HashLength);
        IEncryptionV2 preferred = options.KeyRing is null ? secondary
            : new EncryptionV2Peppered(options.KeyRing, options.Iterations, options.SaltLength, options.HashLength);
        return new MultiEncryption([preferred, secondary], preferred);
    }
}
