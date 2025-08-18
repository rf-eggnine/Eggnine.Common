// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Eggnine.Common;

public interface IMultiEncyrption : IEncryptionV2
{
    /// <summary>
    /// Verify input against 'stored' using a flexible verifier (e.g., CompatEncryption).
    /// If verified and 'stored' is NOT in the target algorithm format, re-hash with 'target'
    /// and persist via callback. Returns true IFF verification succeeds.
    /// </summary>
    /// <param name="input"></param>
    /// <param name="stored"></param>
    /// <param name="persistNewHashAsync"></param>
    /// <returns>true iff input verifies against stored</returns>
    public Task<bool> VerifyAndUpgradeAsync(
        string input,
        string? stored,
        Func<string, Task> persistNewHashAsync);

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
    public Task<(bool Verified, bool Upgraded)> TryVerifyAndUpgradeAsync(
        string input,
        string? stored,
        Func<string, Task> persistNewHashAsync);
}
