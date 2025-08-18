// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;

namespace Eggnine.Common;
public interface IPepperKeyRing
{
    ReadOnlySpan<byte> GetPepper(string keyId);   // throws if unknown
    string CurrentKeyId { get; }                  // e.g., "k1"
}