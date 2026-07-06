// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using System;

namespace Eggnine.Common;

public interface IEncryptionV2 : IDisposable
{
    public string Encrypt(string toEncrypt);

    public bool VerifyEncryption(string toVerify, string encoded);

    /// <summary>
    /// Indicates if one or more algorithms available matches the signature of <see cref="toVerify"/>
    /// </summary>
    /// <param name="toVerify"></param>
    /// <returns></returns>
    public bool IsTargetFormat(string toVerify);

}
