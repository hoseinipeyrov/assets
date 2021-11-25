// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.ImageMagick;

namespace Squidex.Assets
{
    public class MagicThumbnailGeneratorTests : AssetThumbnailGeneratorTests
    {
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
