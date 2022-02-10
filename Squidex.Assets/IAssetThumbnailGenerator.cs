// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public interface IAssetThumbnailGenerator
    {
        bool CanRead(string mimeType)
        {
            return true;
        }

        bool CanWrite(string mimeType)
        {
            return true;
        }

        Task<ImageInfo?> GetImageInfoAsync(Stream source, string mimeType,
            CancellationToken ct = default);

        Task<string?> ComputeBlurHashAsync(Stream source, string mimeType, BlurOptions options,
            CancellationToken ct = default);

        Task FixOrientationAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default);

        Task CreateThumbnailAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
            CancellationToken ct = default);
    }
}
