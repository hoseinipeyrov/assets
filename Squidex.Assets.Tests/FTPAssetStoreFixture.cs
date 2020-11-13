// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using FakeItEasy;
using FluentFTP;
using Microsoft.Extensions.Logging;

namespace Squidex.Assets
{
    public sealed class FTPAssetStoreFixture : IDisposable
    {
        public FTPAssetStore AssetStore { get; }

        public FTPAssetStoreFixture()
        {
            AssetStore = new FTPAssetStore(() => new FtpClient("localhost", 21, "test", "test"), "assets", A.Fake<ILogger<FTPAssetStore>>());
            AssetStore.InitializeAsync().Wait();
        }

        public void Dispose()
        {
        }
    }
}
