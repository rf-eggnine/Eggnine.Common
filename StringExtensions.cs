// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using System.Threading;
using System.Threading.Tasks;

namespace Eggnine.Common;
public static class StringExtensions
{
    internal static IEncryption Encryption {get;set;} = new Encryption();
    public static async Task<string> EncryptAsync(this string toEncrypt, CancellationToken cancellationToken = default) => 
        await Task.Run(() => Encryption.Encrypt(toEncrypt), cancellationToken);
    public static async Task<bool> VerifyEncryptionAsync(this string toVerify, string base64Hash, CancellationToken cancellationToken = default) => 
        await Task.Run(() => Encryption.VerifyEncryption(toVerify, base64Hash), cancellationToken);
}
