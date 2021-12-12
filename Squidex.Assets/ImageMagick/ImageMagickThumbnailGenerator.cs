// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ImageMagick;
using SixLabors.ImageSharp;
using Squidex.Assets.Internal;
using TagLib;
using TagLib.Image;
using static TagLib.File;
using ImageFile = TagLib.Image.File;

namespace Squidex.Assets.ImageMagick
{
    public sealed class ImageMagickThumbnailGenerator : IAssetThumbnailGenerator
    {
        public async Task CreateThumbnailAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(destination, nameof(destination));
            Guard.NotNull(options, nameof(options));

            if (!options.IsValid)
            {
                await source.CopyToAsync(destination, ct);
                return;
            }

            var w = options.TargetWidth ?? 0;
            var h = options.TargetHeight ?? 0;

            using (var collection = new MagickImageCollection())
            {
                await collection.ReadAsync(source, GetFormat(mimeType), ct);

                collection.Coalesce();

                foreach (var image in collection)
                {
                    var clone = image.Clone();

                    var color = options.ParseColor();

                    if (w > 0 || h > 0)
                    {
                        var isCropUpsize = options.Mode == ResizeMode.CropUpsize;

                        var resizeMode = options.Mode;

                        if (isCropUpsize)
                        {
                            resizeMode = ResizeMode.Crop;
                        }

                        if (w >= image.Width && h >= image.Height && resizeMode == ResizeMode.Crop && !isCropUpsize)
                        {
                            resizeMode = ResizeMode.BoxPad;
                        }

                        PointF? centerCoordinates = null;

                        if (options.FocusX.HasValue && options.FocusY.HasValue)
                        {
                            centerCoordinates = new PointF(
                                +(options.FocusX.Value / 2f) + 0.5f,
                                -(options.FocusY.Value / 2f) + 0.5f
                            );
                        }

                        var (size, pad) = ResizeHelper.CalculateTargetLocationAndBounds(resizeMode, new Size(image.Width, image.Height), w, h, centerCoordinates);

                        var sourceRectangle = new MagickGeometry(pad.Width, pad.Height)
                        {
                            IgnoreAspectRatio = true
                        };

                        clone.Resize(sourceRectangle);

                        image.Extent(size.Width, size.Height);
                        image.Clear(color);
                        image.Composite(clone, pad.X, pad.Y, CompositeOperator.Over);
                    }
                    else
                    {
                        image.Clear(color);
                        image.Composite(clone);
                    }

                    image.AutoOrient();

                    if (options.Quality.HasValue)
                    {
                        image.Quality = options.Quality.Value;
                    }
                }

                var firstImage = collection[0];
                var firstFormat = firstImage.Format;

                var targetFormat = options.GetFormat(firstFormat);

                await collection.WriteAsync(destination, targetFormat, ct);
            }
        }

        public async Task<ImageInfo> FixOrientationAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(destination, nameof(destination));

            using (var collection = new MagickImageCollection())
            {
                await collection.ReadAsync(source, GetFormat(mimeType), ct);

                collection.Coalesce();

                foreach (var image in collection)
                {
                    image.AutoOrient();
                }

                await collection.WriteAsync(destination, ct);

                var firstImage = collection[0];

                return new ImageInfo(
                    firstImage.Width,
                    firstImage.Height,
                    firstImage.GetOrientation() > ImageOrientation.TopLeft,
                    firstImage.Format.ToImageFormat());
            }
        }

        public Task<ImageInfo?> GetImageInfoAsync(Stream source, string mimeType,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));

            return Task.FromResult(GetImageInfo(source, mimeType));
        }

        private static ImageInfo? GetImageInfo(Stream source, string mimeType)
        {
            try
            {
                using (var image = new MagickImage())
                {
                    image.Ping(source, new MagickReadSettings
                    {
                        Format = GetFormat(mimeType)
                    });

                    return new ImageInfo(
                        image.Width,
                        image.Height,
                        image.Orientation > OrientationType.TopLeft,
                        image.Format.ToImageFormat());
                }
            }
            catch
            {
                return null;
            }
        }

        private static MagickFormat GetFormat(string mimeType)
        {
            var format = MagickFormat.Unknown;

            if (string.Equals(mimeType, "image/x-tga"))
            {
                format = MagickFormat.Tga;
            }

            return format;
        }
    }
}
