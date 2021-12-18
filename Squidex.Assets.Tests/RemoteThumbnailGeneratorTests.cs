// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Assets.ImageSharp;
using Squidex.Assets.Remote;

namespace Squidex.Assets
{
    public class RemoteThumbnailGeneratorTests : AssetThumbnailGeneratorTests
    {
        protected override HashSet<ImageFormat> SupportedFormats => new HashSet<ImageFormat>
        {
            ImageFormat.BMP,
            ImageFormat.PNG,
            ImageFormat.GIF,
            ImageFormat.JPEG,
            ImageFormat.TGA
        };

        protected override string Name()
        {
            return "remote";
        }

        protected override IAssetThumbnailGenerator CreateSut()
        {
            var serviceCollection = new ServiceCollection();

            serviceCollection.AddHttpClient("Resize", options =>
            {
                options.BaseAddress = new Uri("http://localhost:5005");
            });

            var httpClientFactory = serviceCollection.BuildServiceProvider().GetRequiredService<IHttpClientFactory>();

            return new RemoteThumbnailGenerator(httpClientFactory, new ImageSharpThumbnailGenerator());
        }
    }
}
