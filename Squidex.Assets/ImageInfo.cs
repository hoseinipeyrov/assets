// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.Internal;

namespace Squidex.Assets
{
    public sealed class ImageInfo
    {
        public ImageFormat Format { get; set; }

        public int PixelWidth { get; }

        public int PixelHeight { get; }

        public bool IsRotatedOrSwapped { get; }

        public ImageInfo(int pixelWidth, int pixelHeight, bool isRotatedOrSwapped, ImageFormat format)
        {
            Guard.GreaterThan(pixelWidth, 0, nameof(pixelWidth));
            Guard.GreaterThan(pixelHeight, 0, nameof(pixelHeight));

            Format = format;

            PixelWidth = pixelWidth;
            PixelHeight = pixelHeight;

            IsRotatedOrSwapped = isRotatedOrSwapped;
        }
    }
}
