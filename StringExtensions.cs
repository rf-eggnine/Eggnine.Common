//  ©️ 2024 by RF At EggNine All Rights Reserved

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
