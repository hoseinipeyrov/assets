// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public sealed class AzureBlobAssetStoreFixture
    {
        public AzureBlobAssetStore AssetStore { get; }

        public AzureBlobAssetStoreFixture()
        {
            AssetStore = new AzureBlobAssetStore(new AzureBlobAssetOptions
            {
                ConnectionString = "UseDevelopmentStorage=true",
                ContainerName = "squidex-test-container"
            });
            AssetStore.InitializeAsync(default).Wait();
        }
    }
}
