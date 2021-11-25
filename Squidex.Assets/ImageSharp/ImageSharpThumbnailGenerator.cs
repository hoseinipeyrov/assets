// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.Metadata.Profiles.Exif;
using SixLabors.ImageSharp.Processing;
using Squidex.Assets.Internal;
using ISResizeMode = SixLabors.ImageSharp.Processing.ResizeMode;
using ISResizeOptions = SixLabors.ImageSharp.Processing.ResizeOptions;

namespace Squidex.Assets.ImageSharp
{
    public sealed class ImageSharpThumbnailGenerator : IAssetThumbnailGenerator
    {
        private readonly SemaphoreSlim maxTasks = new SemaphoreSlim(Math.Max(Environment.ProcessorCount / 4, 1));
        private readonly HashSet<string> mimeTypes;

        public ImageSharpThumbnailGenerator()
        {
            mimeTypes = Configuration.Default.ImageFormatsManager.ImageFormats.SelectMany(x => x.MimeTypes).ToHashSet();
        }

        public bool CanRead(string mimeType)
        {
            return CanWrite(mimeType);
        }

        public bool CanWrite(string mimeType)
        {
            return mimeType != null && mimeTypes.Contains(mimeType);
        }

        public async Task CreateThumbnailAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(destination, nameof(destination));
            Guard.NotNull(options, nameof(options));

            if (!options.IsValid)
            {
                await source.CopyToAsync(destination);
                return;
            }

            var w = options.TargetWidth ?? 0;
            var h = options.TargetHeight ?? 0;

            await maxTasks.WaitAsync();
            try
            {
                using (var image = Image.Load(source, out var format))
                {
                    image.Mutate(x => x.AutoOrient());

                    if (w > 0 || h > 0)
                    {
                        var isCropUpsize = options.Mode == ResizeMode.CropUpsize;

                        if (!Enum.TryParse<ISResizeMode>(options.Mode.ToString(), true, out var resizeMode))
                        {
                            resizeMode = ISResizeMode.Max;
                        }

                        if (isCropUpsize)
                        {
                            resizeMode = ISResizeMode.Crop;
                        }

                        if (w >= image.Width && h >= image.Height && resizeMode == ISResizeMode.Crop && !isCropUpsize)
                        {
                            resizeMode = ISResizeMode.BoxPad;
                        }

                        var resizeOptions = new ISResizeOptions { Size = new Size(w, h), Mode = resizeMode, PremultiplyAlpha = true };

                        if (options.FocusX.HasValue && options.FocusY.HasValue)
                        {
                            resizeOptions.CenterCoordinates = new PointF(
                                +(options.FocusX.Value / 2f) + 0.5f,
                                -(options.FocusY.Value / 2f) + 0.5f
                            );
                        }

                        image.Mutate(operation =>
                        {
                            operation = operation.Resize(resizeOptions);

                            if (Color.TryParse(options.Background, out var color))
                            {
                                operation = operation.BackgroundColor(color);
                            }
                            else
                            {
                                operation = operation.BackgroundColor(Color.Transparent);
                            }
                        });
                    }

                    var encoder = options.GetEncoder(format);

                    await image.SaveAsync(destination, encoder);
                }
            }
            finally
            {
                maxTasks.Release();
            }
        }

        public Task<ImageInfo?> GetImageInfoAsync(Stream source, string mimeType,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));

            return Task.FromResult(GetImageInfo(source));
        }

        private static ImageInfo? GetImageInfo(Stream source)
        {
            try
            {
                var image = Image.Identify(source, out var format);

                return image != null ? GetImageInfo(image, format!) : null;
            }
            catch
            {
                return null;
            }
        }

        public async Task<ImageInfo> FixOrientationAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNull(destination, nameof(destination));

            await maxTasks.WaitAsync();
            try
            {
                using (var image = Image.Load(source, out var format))
                {
                    var encoder = Configuration.Default.ImageFormatsManager.FindEncoder(format);

                    if (encoder == null)
                    {
                        throw new NotSupportedException();
                    }

                    image.Mutate(x => x.AutoOrient());

                    await image.SaveAsync(destination, encoder);

                    return GetImageInfo(image, format);
                }
            }
            finally
            {
                maxTasks.Release();
            }
        }

        private static ImageInfo GetImageInfo(IImageInfo image, IImageFormat? detectedFormat)
        {
            var isRotatedOrSwapped = image.Metadata.ExifProfile?.GetValue(ExifTag.Orientation)?.Value > 1;

            var format = ImageFormat.PNG;

            switch (detectedFormat)
            {
                case BmpFormat:
                    format = ImageFormat.BMP;
                    break;
                case JpegFormat:
                    format = ImageFormat.JPEG;
                    break;
                case TgaFormat:
                    format = ImageFormat.TGA;
                    break;
                case GifFormat:
                    format = ImageFormat.GIF;
                    break;
            }

            return new ImageInfo(image.Width, image.Height, isRotatedOrSwapped, format)
            {
                Format = format
            };
        }
    }
}
