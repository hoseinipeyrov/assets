﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Diagnostics.CodeAnalysis;
using Squidex.Assets.Internal;

namespace Squidex.Assets
{
    public abstract class AssetThumbnailGeneratorBase : IAssetThumbnailGenerator
    {
        public virtual bool CanReadAndWrite(string mimeType)
        {
            return true;
        }

        public virtual bool CanComputeBlurHash()
        {
            return true;
        }

        public virtual bool IsResizable(string mimeType, ResizeOptions options, [MaybeNullWhen(false)] out string? destinationMimeType)
        {
            destinationMimeType = null;

            // If we cannot read or write from the mime type we can just stop here.
            if (!CanReadAndWrite(mimeType))
            {
                return false;
            }

            // The mime types are ordered by priority.
            var destinationMimeTypes = options.GetDestinationMimeTypes().Where(CanReadAndWrite).ToArray();

            if ((destinationMimeTypes.Any() && !destinationMimeTypes.Contains(mimeType, StringComparer.OrdinalIgnoreCase)) || options.IsResize || options.Force)
            {
                destinationMimeType = destinationMimeTypes.FirstOrDefault() ?? mimeType;
                return true;
            }

            return false;
        }

        public async Task<ImageInfo?> GetImageInfoAsync(Stream source, string mimeType,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNullOrEmpty(mimeType, nameof(mimeType));

            // If we cannot read or write from the mime type we can just stop here.
            if (!CanReadAndWrite(mimeType))
            {
                return null;
            }

            return await GetImageInfoCoreAsync(source, mimeType, ct);
        }

        protected abstract Task<ImageInfo?> GetImageInfoCoreAsync(Stream source, string mimeType,
            CancellationToken ct = default);

        public async Task<string?> ComputeBlurHashAsync(Stream source, string mimeType, BlurOptions options,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNullOrEmpty(mimeType, nameof(mimeType));
            Guard.NotNull(options, nameof(options));

            // If we cannot read or write from the mime type we can just stop here.
            if (!CanReadAndWrite(mimeType))
            {
                return null;
            }

            return await ComputeBlurHashCoreAsync(source, mimeType, options, ct);
        }

        protected abstract Task<string?> ComputeBlurHashCoreAsync(Stream source, string mimeType, BlurOptions options,
            CancellationToken ct = default);

        public async Task FixOrientationAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNullOrEmpty(mimeType, nameof(mimeType));
            Guard.NotNull(destination, nameof(destination));

            // If we cannot read or write from the mime type we can just stop here.
            if (!CanReadAndWrite(mimeType))
            {
                await source.CopyToAsync(destination, ct);
                return;
            }

            await FixOrientationCoreAsync(source, mimeType, destination, ct);
        }

        protected abstract Task FixOrientationCoreAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default);

        public async Task CreateThumbnailAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
            CancellationToken ct = default)
        {
            Guard.NotNull(source, nameof(source));
            Guard.NotNullOrEmpty(mimeType, nameof(mimeType));
            Guard.NotNull(destination, nameof(destination));
            Guard.NotNull(options, nameof(options));

            if (!IsResizable(mimeType, options, out _))
            {
                await source.CopyToAsync(destination, ct);
                return;
            }

            // The mime types are ordered by priority.
            var destinationMimeTypes = options.GetDestinationMimeTypes().Where(CanReadAndWrite).ToList();

            // The current mime type is also an option.
            destinationMimeTypes.Add(mimeType);

            await CreateThumbnailCoreAsync(source, mimeType, destinationMimeTypes, destination, options, ct);
        }

        protected abstract Task CreateThumbnailCoreAsync(Stream source, string mimeType, IReadOnlyList<string> destinationMimeTypes, Stream destination, ResizeOptions options,
            CancellationToken ct = default);
    }
}
