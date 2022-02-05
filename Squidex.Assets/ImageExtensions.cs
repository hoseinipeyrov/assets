// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public static class ImageExtensions
    {
        public static string ToMimeType(this ImageFormat format)
        {
            switch (format)
            {
                case ImageFormat.BMP:
                    return "image/bmp";
                case ImageFormat.GIF:
                    return "image/gif";
                case ImageFormat.JPEG:
                    return "image/jpeg";
                case ImageFormat.PNG:
                    return "image/png";
                case ImageFormat.TGA:
                    return "image/x-tga";
                case ImageFormat.TIFF:
                    return "image/tiff";
                case ImageFormat.WEBP:
                    return "image/webp";
                default:
                    throw new ArgumentException("Invalid format.", nameof(format));
            }
        }
    }
}
