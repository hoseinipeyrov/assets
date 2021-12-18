﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Squidex.Assets.ImageMagick;
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
            ImageFormat.TGA,
            ImageFormat.TIFF,
            ImageFormat.WEBP
        };

        protected override string Name()
        {
            return "remote";
        }

        protected override IAssetThumbnailGenerator CreateSut()
        {
            var services =
                new ServiceCollection()
                .AddHttpClient("Resize", options =>
                {
                    options.BaseAddress = new Uri("https://squidex-resizer-public-ylxt3nzaaq-ew.a.run.app/");
                }).Services
                .BuildServiceProvider();

            var httpClientFactory = services.GetRequiredService<IHttpClientFactory>();

            var inner = new CompositeThumbnailGenerator(new IAssetThumbnailGenerator[]
            {
                new ImageSharpThumbnailGenerator(),
                new ImageMagickThumbnailGenerator()
            });

            return new RemoteThumbnailGenerator(httpClientFactory, inner);
        }
    }
}
