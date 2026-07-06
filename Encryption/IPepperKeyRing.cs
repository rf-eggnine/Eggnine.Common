// Copyright © 2025 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System;

namespace Eggnine.Common;
public interface IPepperKeyRing
{
    ReadOnlySpan<byte> GetPepper(string keyId);   // throws if unknown
    string CurrentKeyId { get; }                  // e.g., "k1"
}