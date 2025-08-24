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
        IMultiEncryption multiEncyrption = IMultiEncryption.GetMultiEncryption(encryptionOptions);
        serviceCollection.AddSingleton<IEncryption>(multiEncyrption);
        serviceCollection.AddSingleton<IEncryptionV2>(multiEncyrption);
        serviceCollection.AddSingleton(multiEncyrption);
        return serviceCollection;
    }
}