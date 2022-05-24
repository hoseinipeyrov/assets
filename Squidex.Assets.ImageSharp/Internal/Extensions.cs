// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;

namespace Squidex.Assets.Internal
{
    internal static class Extensions
    {
        public static IImageEncoder GetEncoder(this ResizeOptions options, string[] mimeTypes, IImageFormat format)
        {
            var imageFormatsManager = Configuration.Default.ImageFormatsManager;

            foreach (var mimeType in mimeTypes)
            {
                var mimeTypeFormat = imageFormatsManager.FindFormatByMimeType(mimeType);

                // Use the best matching format.
                if (mimeTypeFormat != null)
                {
                    format = mimeTypeFormat;
                    break;
                }
            }

            var encoder = imageFormatsManager.FindEncoder(format);

            if (encoder == null)
            {
                throw new NotSupportedException();
            }

            if (encoder is PngEncoder png && png.ColorType != PngColorType.RgbWithAlpha)
            {
                encoder = new PngEncoder
                {
                    ColorType = PngColorType.RgbWithAlpha
                };
            }

            var quality = options.Quality ?? 80;

            if (encoder is JpegEncoder jpg && jpg.Quality != quality)
            {
                encoder = new JpegEncoder
                {
                    Quality = quality
                };
            }

            return encoder;
        }
    }
}
