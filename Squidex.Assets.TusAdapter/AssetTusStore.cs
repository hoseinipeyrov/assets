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
        private readonly IKeyValueStore keyValueStore;

        public AssetTusStore(IAssetStore assetStore, IKeyValueStore keyValueStore)
        {
            this.assetStore = assetStore;
            this.keyValueStore = keyValueStore;
        }

        public async Task<string> CreateFileAsync(long uploadLength, string metadata,
            CancellationToken cancellationToken)
        {
            var id = Guid.NewGuid().ToString();

            var metadataObj = new Metadata
            {
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

            if (metadata.Parts == 0)
            {
                return null;
            }

            if (files.TryGetValue(fileId, out var file))
            {
                return await file;
            }

            async Task<AssetTusFile> CreateFileAsync(string fileId, Metadata metadata, CancellationToken cancellationToken)
            {
                var tempPath = Path.Combine(Path.GetTempPath(), Key(fileId));

                var tempStream = new FileStream(tempPath,
                    FileMode.Create,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    4096,
                    FileOptions.DeleteOnClose);

                for (var i = 0; i < metadata.Parts; i++)
                {
                    await assetStore.DownloadAsync(PartName(fileId, i), tempStream, default, cancellationToken);
                }

                return new AssetTusFile(fileId, metadata, tempStream, x =>
                {
                    files.TryRemove(x.Id, out _);
                });
            }

            file = CreateFileAsync(fileId, metadata, cancellationToken);

            files[fileId] = file;

            return await file;
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

            if (stream.Length > 0 && metadata.UploadLength.HasValue)
            {
                var sizeAfterUpload = metadata.UploadLength + stream.Length;

                if (metadata.UploadLength + stream.Length > metadata.UploadLength.Value)
                {
                    throw new TusStoreException($"Stream contains more data than the file's upload length. Stream data: {sizeAfterUpload}, upload length: {metadata.UploadLength}.");
                }
            }

            var partName = PartName(fileId, metadata.Parts);
            var partSize = -1L;
            try
            {
                var cancellableStream = new CancellableStream(stream, cancellationToken);

                partSize = await assetStore.UploadAsync(partName, cancellableStream, false, default);
            }
            catch (OperationCanceledException)
            {
            }

            if (partSize < 0)
            {
                partSize = await assetStore.GetSizeAsync(partName, cancellationToken);
            }

            metadata.BytesWritten += partSize;
            metadata.Parts++;

            if (metadata.UploadLength.HasValue && metadata.BytesWritten > metadata.UploadLength.Value)
            {
                throw new TusStoreException($"Stream contains more data than the file's upload length. Stream data: {metadata.BytesWritten}, upload length: {metadata.UploadLength}.");
            }

            await SetMetadataAsync(fileId, metadata, cancellationToken);

            return partSize;
        }

        public async Task<bool> FileExistAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata.BytesWritten > 0 || metadata.Created;
        }

        public async Task<IEnumerable<string>> GetExpiredFilesAsync(
            CancellationToken cancellationToken)
        {
            var result = new List<string>();

            var expirations = keyValueStore.GetExpiredEntriesAsync<Metadata>(DateTimeOffset.UtcNow, cancellationToken);

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

            return metadata.BytesWritten;
        }

        public async Task<DateTimeOffset?> GetExpirationAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata.Expiration;
        }

        private Task<Metadata> GetMetadataAsync(string fileId,
            CancellationToken ct)
        {
            var key = Key(fileId);

            return keyValueStore.GetAsync<Metadata>(key, ct);
        }

        public async Task<int> RemoveExpiredFilesAsync(
            CancellationToken cancellationToken)
        {
            var deletionCount = 0;
            var expirations = keyValueStore.GetExpiredEntriesAsync<Metadata>(DateTimeOffset.UtcNow, cancellationToken);

            await foreach (var (key, expiration) in expirations.WithCancellation(cancellationToken))
            {
                for (var i = 0; i < expiration.Parts; i++)
                {
                    await assetStore.DeleteAsync(PartName(expiration.Id, i), cancellationToken);
                }

                await keyValueStore.DeleteAsync(key, cancellationToken);
                deletionCount++;
            }

            return deletionCount;
        }

        private Task SetMetadataAsync(string fileId, Metadata metadata,
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
