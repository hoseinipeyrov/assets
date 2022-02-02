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
            AssetStore = new FTPAssetStore(() => new FtpClient(
                TestHelpers.Configuration["ftp:serverHost"], 21,
                TestHelpers.Configuration["ftp:username"],
                TestHelpers.Configuration["ftp:userPassword"]), new FTPAssetOptions
            {
                Path = TestHelpers.Configuration["ftp:path"]
            }, A.Fake<ILogger<FTPAssetStore>>());
            AssetStore.InitializeAsync(default).Wait();
        }

        public void Dispose()
        {
        }
    }
}
