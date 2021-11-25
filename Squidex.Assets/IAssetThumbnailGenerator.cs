// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.IO;
using System.Threading;
using System.Threading.Tasks;

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

        Task<ImageInfo> FixOrientationAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default);

        Task CreateThumbnailAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
            CancellationToken ct = default);
    }
}
