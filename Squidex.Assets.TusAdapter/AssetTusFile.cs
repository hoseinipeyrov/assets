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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using tusdotnet.Interfaces;
using tusdotnet.Models;

namespace Squidex.Assets
{
    public sealed class AssetTusFile : AssetFile, ITusFile, IDisposable, IAsyncDisposable
    {
        private readonly Dictionary<string, Metadata> parsedMetadata;
        private readonly Stream stream;
        private readonly Action<AssetTusFile> disposed;

        public string Id { get; }

        internal TusMetadata Metadata { get; }

        public AssetTusFile(string id, TusMetadata metadata, Dictionary<string, Metadata> parsedMetadata, Stream stream, Action<AssetTusFile> disposed)
            : base(GetFileName(parsedMetadata), GetMimeType(parsedMetadata), stream.Length)
        {
            Id = id;

            this.parsedMetadata = parsedMetadata;
            this.stream = stream;
            this.disposed = disposed;

            Metadata = metadata;
        }

        private static string GetFileName(Dictionary<string, Metadata> metadata)
        {
            var result = string.Empty;

            var fileName = metadata.FirstOrDefault(x => string.Equals(x.Key, "fileName", StringComparison.OrdinalIgnoreCase)).Value;

            if (fileName != null)
            {
                result = fileName.GetString(Encoding.UTF8);
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                result = "Unknown";
            }

            return result;
        }

        private static string GetMimeType(Dictionary<string, Metadata> metadata)
        {
            var result = string.Empty;

            var fileType = metadata.FirstOrDefault(x => string.Equals(x.Key, "fileType", StringComparison.OrdinalIgnoreCase)).Value;

            if (fileType != null)
            {
                result = fileType.GetString(Encoding.UTF8);
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                fileType = metadata.FirstOrDefault(x => string.Equals(x.Key, "mimeType", StringComparison.OrdinalIgnoreCase)).Value;

                if (fileType != null)
                {
                    return fileType.GetString(Encoding.UTF8);
                }
            }

            if (string.IsNullOrWhiteSpace(result))
            {
                result = "application/octet-stream";
            }

            return result;
        }

        public override void Dispose()
        {
            disposed(this);

            stream.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            disposed(this);

            return stream.DisposeAsync();
        }

        public override Stream OpenRead()
        {
            return new NonDisposingStream(stream);
        }

        public Task<Stream> GetContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new NonDisposingStream(stream));
        }

        public Task<Dictionary<string, Metadata>> GetMetadataAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(parsedMetadata);
        }
    }
}
