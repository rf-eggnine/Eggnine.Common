// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System;

namespace Eggnine.Common;

public interface IEncryptionV2 : IEncryption, IDisposable
{
    /// <summary>
    /// Indicates if one or more algorithms available matches the signature of <see cref="toVerify"/>
    /// </summary>
    /// <param name="toVerify"></param>
    /// <returns></returns>
    public bool IsTargetFormat(string toVerify);

}
