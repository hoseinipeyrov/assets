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

namespace Squidex.Assets
{
    public sealed class CompositeThumbnailGenerator : IAssetThumbnailGenerator
    {
        private readonly SemaphoreSlim maxTasks;
        private readonly IEnumerable<IAssetThumbnailGenerator> inners;

        public CompositeThumbnailGenerator(IEnumerable<IAssetThumbnailGenerator> inners, int maxTasks = 0)
        {
            if (maxTasks <= 0)
            {
                maxTasks = Math.Max(Environment.ProcessorCount / 4, 1);
            }

            this.maxTasks = new SemaphoreSlim(maxTasks);

            this.inners = inners;
        }

        public bool CanRead(string mimeType)
        {
            return mimeType != null && inners.Any(x => x.CanRead(mimeType));
        }

        public bool CanWrite(string mimeType)
        {
            return mimeType != null && inners.Any(x => x.CanWrite(mimeType));
        }

        public async Task CreateThumbnailAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
            CancellationToken ct = default)
        {
            await maxTasks.WaitAsync(ct);
            try
            {
                var destinationMimeTime = mimeType;

                if (options.Format.HasValue)
                {
                    destinationMimeTime = options.Format.Value.ToMimeType();
                }

                foreach (var inner in inners)
                {
                    if (inner.CanWrite(mimeType) && inner.CanWrite(destinationMimeTime))
                    {
                        await inner.CreateThumbnailAsync(source, mimeType, destination, options, ct);
                        return;
                    }
                }
            }
            finally
            {
                maxTasks.Release();
            }

            await source.CopyToAsync(destination, ct);
        }

        public async Task FixOrientationAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default)
        {
            await maxTasks.WaitAsync(ct);
            try
            {
                foreach (var inner in inners)
                {
                    if (inner.CanRead(mimeType) && inner.CanRead(mimeType))
                    {
                        await inner.FixOrientationAsync(source, mimeType, destination, ct);
                        return;
                    }
                }
            }
            finally
            {
                maxTasks.Release();
            }

            throw new InvalidOperationException("No thumbnail generator registered.");
        }

        public async Task<ImageInfo?> GetImageInfoAsync(Stream source, string mimeType,
            CancellationToken ct = default)
        {
            foreach (var inner in inners)
            {
                var result = await inner.GetImageInfoAsync(source, mimeType, ct);

                if (result != null)
                {
                    return result;
                }

                source.Position = 0;
            }

            return null;
        }
    }
}
