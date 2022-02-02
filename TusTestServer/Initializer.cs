// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets;

namespace TutTestServer
{
    public sealed class Initializer : IHostedService
    {
        private readonly IAssetStore assetStore;
        private readonly IAssetKeyValueStore<TusMetadata> assetKeyValueStore;

        public Initializer(IAssetStore assetStore, IAssetKeyValueStore<TusMetadata> assetKeyValueStore)
        {
            this.assetStore = assetStore;
            this.assetKeyValueStore = assetKeyValueStore;
        }

        public async Task StartAsync(
            CancellationToken cancellationToken)
        {
            await assetStore.InitializeAsync(cancellationToken);
            await assetKeyValueStore.InitializeAsync(cancellationToken);
        }

        public Task StopAsync(
            CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
