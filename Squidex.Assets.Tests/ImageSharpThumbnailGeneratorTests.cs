// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Generic;
using Squidex.Assets.ImageSharp;

namespace Squidex.Assets
{
    public class ImageSharpThumbnailGeneratorTests : AssetThumbnailGeneratorTests
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
            return "imagesharp";
        }

        protected override IAssetThumbnailGenerator CreateSut()
        {
            return new ImageSharpThumbnailGenerator();
        }
    }
}
