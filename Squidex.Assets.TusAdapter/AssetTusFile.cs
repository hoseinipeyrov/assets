// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Parsers;
using TusMetadata = tusdotnet.Models.Metadata;

namespace Squidex.Assets
{
    public sealed class AssetTusFile : ITusFile, IDisposable, IAsyncDisposable
    {
        private readonly Lazy<Dictionary<string, TusMetadata>> metadata;
        private readonly Stream stream;
        private readonly Action<AssetTusFile> disposed;

        public string Id { get; }

        internal Metadata Metadata { get; }

        public AssetTusFile(string id, Metadata metadata, Stream stream, Action<AssetTusFile> disposed)
        {
            Id = id;

            this.stream = stream;
            this.disposed = disposed;
            this.metadata = new Lazy<Dictionary<string, TusMetadata>>(() =>
            {
                return MetadataParser.ParseAndValidate(MetadataParsingStrategy.AllowEmptyValues, metadata.UploadMetadata).Metadata;
            });

            Metadata = metadata;
        }

        public void Dispose()
        {
            disposed(this);

            stream.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            disposed(this);

            return stream.DisposeAsync();
        }

        public Task<Stream> GetContentAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new NonDisposingStream(stream));
        }

        public Task<Dictionary<string, TusMetadata>> GetMetadataAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(metadata.Value);
        }
    }
}
