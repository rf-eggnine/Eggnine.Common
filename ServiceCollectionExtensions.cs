// ©️ 2025 RF@Eggnine.com
// Licensed under the EG9-PD License which includes a personal IP disclaimer.
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Eggnine.Common;

public static class ServiceCollectionExtensions
{
    public static T AddEncryption<T>(this T serviceCollection, EncryptionOptions encryptionOptions)
        where T : IServiceCollection
    {
        IEncryption legacy = new Encryption(encryptionOptions.Iterations, encryptionOptions.SaltLength);
        IEncryptionV2 v2 = new EncryptionV2(encryptionOptions.Iterations, encryptionOptions.SaltLength, encryptionOptions.HashLength);
        IEncryptionV2 v2Pepper = new EncryptionV2Peppered(encryptionOptions.keyRing!, encryptionOptions.Iterations, encryptionOptions.SaltLength, encryptionOptions.HashLength);
        IMultiEncyrption multiEncyrption = new MultiEncryption([v2Pepper, v2], v2Pepper, legacy);
        serviceCollection.AddSingleton(multiEncyrption);
        return serviceCollection;
    }
}