// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Eggnine.Common;

public struct EncryptionOptions
{
    public EncryptionOptions() { }
    public IPepperKeyRing? KeyRing { get; set; }
    public int Iterations { get; set; } = 200_000;
    public int SaltLength { get; set; } = 16;
    public int HashLength { get; set; } = 32;
}