﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public class ImageMagickThumbnailGeneratorTests : AssetThumbnailGeneratorTests
    {
        protected override bool SupportsBlurHash => false;

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
            return "magick";
        }

        protected override IAssetThumbnailGenerator CreateSut()
        {
            return new ImageMagickThumbnailGenerator();
        }
    }
}
