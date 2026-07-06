// Copyright © 2025-2026 RF@Eggnine.com
// Licensed under the MIT License. See LICENSE file in the project root.

using Microsoft.Extensions.DependencyInjection;

namespace Eggnine.Common;

public static class ServiceCollectionExtensions
{
    public static T AddEncryption<T>(this T serviceCollection, EncryptionOptions encryptionOptions)
        where T : IServiceCollection
    {
        IMultiEncryption multiEncyrption = IMultiEncryption.GetMultiEncryption(encryptionOptions);
        serviceCollection.AddSingleton<IEncryptionV2>(multiEncyrption);
        serviceCollection.AddSingleton(multiEncyrption);
        return serviceCollection;
    }
}