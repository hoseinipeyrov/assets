// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Concurrent;
using System.Security.Cryptography;
using Squidex.Assets.Internal;
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
        ITusTerminationStore,
        ITusStore
    {
        private static readonly TimeSpan DefaultExpiration = TimeSpan.FromDays(2);
        private readonly ConcurrentDictionary<string, Task<AssetTusFile>> files = new ConcurrentDictionary<string, Task<AssetTusFile>>();
        private readonly IAssetStore assetStore;
        private readonly IAssetKeyValueStore<TusMetadata> assetKeyValueStore;

        public AssetTusStore(IAssetStore assetStore, IAssetKeyValueStore<TusMetadata> keyValueStore)
        {
            this.assetStore = assetStore;
            this.assetKeyValueStore = keyValueStore;
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
                Expires = DateTimeOffset.UtcNow.Add(DefaultExpiration)
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
            var metadata = (await GetMetadataAsync(fileId, cancellationToken)) ?? new TusMetadata();

            metadata.Expires = expires;

            await SetMetadataAsync(fileId, metadata, cancellationToken);
        }

        public async Task SetUploadLengthAsync(string fileId, long uploadLength,
            CancellationToken cancellationToken)
        {
            var metadata = (await GetMetadataAsync(fileId, cancellationToken)) ?? new TusMetadata();

            metadata.UploadLength = uploadLength;

            await SetMetadataAsync(fileId, metadata, cancellationToken);
        }

        public async Task<ITusFile?> GetFileAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            if (metadata == null || metadata.WrittenParts == 0)
            {
                return null;
            }

            async Task<AssetTusFile> CreateFileAsync(string fileId, TusMetadata metadata,
                CancellationToken ct)
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
                    await assetStore.DownloadAsync(PartName(fileId, i), tempStream, default, ct);
                }

                var parsedMetadata = MetadataParser.ParseAndValidate(MetadataParsingStrategy.AllowEmptyValues, metadata.UploadMetadata).Metadata;

                await CleanupAsync(metadata, default);

                return new AssetTusFile(fileId, metadata, parsedMetadata, tempStream, x =>
                {
                    files.TryRemove(x.Id, out _);
                });
            }

#pragma warning disable MA0106 // Avoid closure by using an overload with the 'factoryArgument' parameter
            return await files.GetOrAdd(fileId, (x, args) =>
            {
                return CreateFileAsync(x, args.metadata, args.cancellationToken);
            }, (metadata, cancellationToken));
#pragma warning restore MA0106 // Avoid closure by using an overload with the 'factoryArgument' parameter
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
                    var calculateSha1 = await sha1.ComputeHashAsync(dataStream, cancellationToken);

                    return checksum.SequenceEqual(calculateSha1);
                }
            }
        }

        public async Task<long> AppendDataAsync(string fileId, Stream stream,
            CancellationToken cancellationToken)
        {
            var metadata = (await GetMetadataAsync(fileId, cancellationToken)) ?? new TusMetadata();

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

            return metadata != null && (metadata.WrittenBytes > 0 || metadata.Created);
        }

        public async Task<IEnumerable<string>> GetExpiredFilesAsync(
            CancellationToken cancellationToken)
        {
            var result = new List<string>();

            var expirations = assetKeyValueStore.GetExpiredEntriesAsync(DateTimeOffset.UtcNow, cancellationToken);

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

            return metadata?.UploadLength;
        }

        public async Task<string?> GetUploadMetadataAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata?.UploadMetadata;
        }

        public async Task<long> GetUploadOffsetAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata?.WrittenBytes ?? 0;
        }

        public async Task<DateTimeOffset?> GetExpirationAsync(string fileId,
            CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            return metadata?.Expires;
        }

        private async Task<TusMetadata?> GetMetadataAsync(string fileId,
            CancellationToken ct)
        {
            var key = Key(fileId);

            return await assetKeyValueStore.GetAsync(key, ct);
        }

        public async Task<int> RemoveExpiredFilesAsync(
            CancellationToken cancellationToken)
        {
            var deletionCount = 0;

            var expirations = assetKeyValueStore.GetExpiredEntriesAsync(DateTimeOffset.UtcNow, cancellationToken);

            await foreach (var (_, expiration) in expirations.WithCancellation(cancellationToken))
            {
                await CleanupAsync(expiration, cancellationToken);
            }

            return deletionCount;
        }

        public async Task DeleteFileAsync(string fileId, CancellationToken cancellationToken)
        {
            var metadata = await GetMetadataAsync(fileId, cancellationToken);

            if (metadata == null)
            {
                return;
            }

            await CleanupAsync(metadata, cancellationToken);
        }

        private async Task CleanupAsync(TusMetadata metadata, CancellationToken cancellationToken)
        {
            for (var i = 0; i < metadata.WrittenParts; i++)
            {
                await assetStore.DeleteAsync(PartName(metadata.Id, i), cancellationToken);
            }

            await assetKeyValueStore.DeleteAsync(Key(metadata.Id), cancellationToken);
        }

        private Task SetMetadataAsync(string fileId, TusMetadata metadata,
            CancellationToken ct)
        {
            var key = Key(fileId);

            metadata.Id = fileId;

            if (metadata.Expires == default)
            {
                metadata.Expires = DateTimeOffset.UtcNow.Add(DefaultExpiration);
            }

            return assetKeyValueStore.SetAsync(key, metadata, metadata.Expires!.Value, ct);
        }

        private static string PartName(string fileId, int index)
        {
            return $"{fileId}_{index}";
        }

        private static string Key(string fileId)
        {
            return $"TUSFILE_{fileId}";
        }
    }
}
