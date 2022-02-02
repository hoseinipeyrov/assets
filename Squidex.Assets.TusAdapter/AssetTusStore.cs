// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Parsers;

namespace Squidex.Assets
{
    public sealed class AssetTusStore :
        ITusExpirationStore,
        ITusChecksumStore,
        ITusCreationDeferLengthStore,
        ITusCreationStore,
        ITusReadableStore,
        ITusStore
    {
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(2);
        private readonly ConcurrentDictionary<string, Task<AssetTusFile>> files = new ConcurrentDictionary<string, Task<AssetTusFile>>();
        private readonly IAssetStore assetStore;
        private readonly IAssetKeyValueStore<TusMetadata> keyValueStore;

        public AssetTusStore(IAssetStore assetStore, IAssetKeyValueStore<TusMetadata> keyValueStore)
        {
            this.assetStore = assetStore;
            this.keyValueStore = keyValueStore;
        }

        public async Task<string> CreateFileAsync(long uploadLength, string metadata,
            CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid().ToString();

            var metadataObj = new TusMetadata
            {
                Created = true,
                UploadLength = uploadLength,
                UploadMetadata = metadata,
                Expiration = DateTimeOffset.UtcNow.Add(DefaultExpiration)
            };

            await SetMetadataAsync(id, metadataObj, cancellationToken);

            return id;
        }

        public Task<IEnumerable<string>> GetSupportedAlgorithmsAsync(
            CancellationToken cancellationToken)
        {
            return Task.FromResult<IEnumerable<string>>(new[] { "sha1" });
        }

        public async Task SetExpirationAsync(string fileId, DateTimeOffset expires,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            metadata.Expiration = expires;

            await SetMetadataAsync(fileId, metadata, cancellationToken);
        }

        public async Task SetUploadLengthAsync(string fileId, long uploadLength,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            metadata.UploadLength = uploadLength;

            await SetMetadataAsync(fileId, metadata, cancellationToken);
        }

        public async Task<ITusFile> GetFileAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            if (metadata.WrittenParts == 0)
            {
                return null;
            }

            async Task<AssetTusFile> CreateFileAsync(string fileId, TusMetadata metadata, CancellationToken cancellationToken)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Key(fileId));

                var tempStream = new FileStream(tempPath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    4096,
                    FileOptions.DeleteOnClose);

                for (var i = 0; i < metadata.WrittenParts; i++)
                {
                    await assetStore.DownloadAsync(PartName(fileId, i), tempStream, default, cancellationToken);
                }

                var parsedMetadata = MetadataParser.ParseAndValidate(MetadataParsingStrategy.AllowEmptyValues, metadata.UploadMetadata).Metadata;

                await CleanupAsync(metadata, default);

                return new AssetTusFile(fileId, metadata, parsedMetadata, tempStream, x =>
                {
                    files.TryRemove(x.Id, out _);
                });
            }

            return await files.GetOrAdd(fileId, x =>
            {
                return CreateFileAsync(x, metadata, cancellationToken);
            });
        }

        public async Task<bool> VerifyChecksumAsync(string fileId, string algorithm, byte[] checksum,
            CancellationToken cancellationToken)
        {
            var file = await GetFileAsync(fileId, cancellationToken);

            if (file == null)
            {
                return false;
            }

            await using (var dataStream = await file.GetContentAsync(cancellationToken))
            {
                using (var sha1 = SHA1.Create())
                {
                    var calculateSha1 = sha1.ComputeHash(dataStream);

                    return checksum.SequenceEqual(calculateSha1);
                }
            }
        }

        public async Task<long> AppendDataAsync(string fileId, Stream stream,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            if (stream.GetLengthOrZero() > 0 && metadata.UploadLength.HasValue)
            {
                var sizeAfterUpload = metadata.UploadLength + stream.GetLengthOrZero();

                if (metadata.UploadLength + stream.Length > metadata.UploadLength.Value)
                {
                    throw new TusStoreException($"Stream contains more data than the file's upload length. Stream data: {sizeAfterUpload}, upload length: {metadata.UploadLength}.");
                }
            }

            var partName = PartName(fileId, metadata.WrittenParts);
            var partSize = -1L;
            try
            {
                var cancellableStream = new CancellableStream(stream, cancellationToken);

                partSize = await assetStore.UploadAsync(partName, cancellableStream, true, default);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                // Do not flow cancellation token here so we can save the metadata even if the request is aborted.
                if (partSize < 0)
                {
                    partSize = await assetStore.GetSizeAsync(partName, default);
                }

                metadata.WrittenBytes += partSize;
                metadata.WrittenParts++;

                await SetMetadataAsync(fileId, metadata, default);
            }

            if (metadata.UploadLength.HasValue && metadata.WrittenBytes > metadata.UploadLength.Value)
            {
                throw new TusStoreException($"Stream contains more data than the file's upload length. Stream data: {metadata.WrittenBytes}, upload length: {metadata.UploadLength}.");
            }

            return partSize;
        }

        public async Task<bool> FileExistAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata.WrittenBytes > 0 || metadata.Created;
        }

        public async Task<IEnumerable<string>> GetExpiredFilesAsync(
            CancellationToken cancellationToken)
        {
            var result = new List<string>();

            var expirations = keyValueStore.GetExpiredEntriesAsync(DateTimeOffset.UtcNow, cancellationToken);

            await foreach (var (_, value) in expirations.WithCancellation(cancellationToken))
            {
                result.Add(value.Id);
            }

            return result;
        }

        public async Task<long?> GetUploadLengthAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata.UploadLength;
        }

        public async Task<string> GetUploadMetadataAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata.UploadMetadata;
        }

        public async Task<long> GetUploadOffsetAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata.WrittenBytes;
        }

        public async Task<DateTimeOffset?> GetExpirationAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata.Expiration;
        }

        private Task<TusMetadata> GetMetadataAsync(string fileId,
            CancellationToken ct)
        {
            var key = Key(fileId);

            return keyValueStore.GetAsync(key, ct);
        }

        public async Task<int> RemoveExpiredFilesAsync(
            CancellationToken cancellationToken)
        {
            var deletionCount = 0;

            var expirations = keyValueStore.GetExpiredEntriesAsync(DateTimeOffset.UtcNow, cancellationToken);

            await foreach (var (_, expiration) in expirations.WithCancellation(cancellationToken))
            {
                await CleanupAsync(expiration, cancellationToken);
            }

            return deletionCount;
        }

        private async Task CleanupAsync(TusMetadata metadata, CancellationToken cancellationToken)
        {
            for (var i = 0; i < metadata.WrittenParts; i++)
            {
                await assetStore.DeleteAsync(PartName(metadata.Id, i), cancellationToken);
            }

            await keyValueStore.DeleteAsync(Key(metadata.Id), cancellationToken);
        }

        private Task SetMetadataAsync(string fileId, TusMetadata metadata,
            CancellationToken ct)
        {
            var key = Key(fileId);

            metadata.Id = fileId;

            if (metadata.Expiration == default)
            {
                metadata.Expiration = DateTimeOffset.UtcNow.Add(DefaultExpiration);
            }

            return keyValueStore.SetAsync(key, metadata, metadata.Expiration.Value, ct);
        }

        private static string PartName(string fileId, int index)
        {
            return $"{fileId}_{index}";
        }

        private string Key(string fileId)
        {
            return $"TUSFILE_{fileId}";
        }
    }
}
