// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

namespace Eggnine.Common;

public interface IEncryption
{
    public string Encrypt(string toEncrypt);

    public bool VerifyEncryption(string toVerify, string base64Hash);
}
