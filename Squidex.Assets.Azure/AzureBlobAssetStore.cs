﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Squidex.Assets.Internal;

namespace Squidex.Assets
{
    public class AzureBlobAssetStore : IAssetStore
    {
        private readonly string containerName;
        private readonly string connectionString;
        private CloudBlobContainer blobContainer;

        public AzureBlobAssetStore(string connectionString, string containerName)
        {
            Guard.NotNullOrEmpty(containerName, nameof(containerName));
            Guard.NotNullOrEmpty(connectionString, nameof(connectionString));

            this.connectionString = connectionString;
            this.containerName = containerName;
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            try
            {
                var storageAccount = CloudStorageAccount.Parse(connectionString);

                var blobClient = storageAccount.CreateCloudBlobClient();
                var blobReference = blobClient.GetContainerReference(containerName);

                await blobReference.CreateIfNotExistsAsync(ct);

                blobContainer = blobReference;
            }
            catch (Exception ex)
            {
                throw new AssetStoreException($"Cannot connect to blob container '{containerName}'.", ex);
            }
        }

        public string GeneratePublicUrl(string fileName)
        {
            var name = GetFileName(fileName, nameof(fileName));

            if (blobContainer.Properties.PublicAccess != BlobContainerPublicAccessType.Blob)
            {
                var blob = blobContainer.GetBlockBlobReference(name);

                return blob.Uri.ToString();
            }

            return null;
        }

        public async Task<long> GetSizeAsync(string fileName, CancellationToken ct = default)
        {
            var name = GetFileName(fileName, nameof(fileName));

            try
            {
                var blob = blobContainer.GetBlockBlobReference(name);

                await blob.FetchAttributesAsync(ct);

                return blob.Properties.Length;
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                throw new AssetNotFoundException(fileName, ex);
            }
        }

        public async Task CopyAsync(string sourceFileName, string targetFileName, CancellationToken ct = default)
        {
            var sourceName = GetFileName(sourceFileName, nameof(sourceFileName));
            var targetName = GetFileName(targetFileName, nameof(targetFileName));

            try
            {
                var sourceBlob = blobContainer.GetBlockBlobReference(sourceName);
                var targetBlob = blobContainer.GetBlobReference(targetName);

                await targetBlob.StartCopyAsync(sourceBlob.Uri, null, AccessCondition.GenerateIfNotExistsCondition(), null, null, ct);

                while (targetBlob.CopyState.Status == CopyStatus.Pending)
                {
                    ct.ThrowIfCancellationRequested();

                    await Task.Delay(50, ct);
                    await targetBlob.FetchAttributesAsync(null, null, null, ct);
                }

                if (targetBlob.CopyState.Status != CopyStatus.Success)
                {
                    throw new StorageException($"Copy of temporary file failed: {targetBlob.CopyState.Status}");
                }
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 409)
            {
                throw new AssetAlreadyExistsException(targetFileName, ex);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                throw new AssetNotFoundException(sourceFileName, ex);
            }
        }

        public async Task DownloadAsync(string fileName, Stream stream, BytesRange range = default, CancellationToken ct = default)
        {
            Guard.NotNull(stream, nameof(stream));

            var name = GetFileName(fileName, nameof(fileName));

            try
            {
                var blob = blobContainer.GetBlockBlobReference(name);

                using (var blobStream = await blob.OpenReadAsync(null, null, null, ct))
                {
                    await blobStream.CopyToAsync(stream, range, ct);
                }
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 404)
            {
                throw new AssetNotFoundException(fileName, ex);
            }
        }

        public async Task UploadAsync(string fileName, Stream stream, bool overwrite = false, CancellationToken ct = default)
        {
            Guard.NotNull(stream, nameof(stream));

            var name = GetFileName(fileName, nameof(fileName));

            try
            {
                var tempBlob = blobContainer.GetBlockBlobReference(name);

                await tempBlob.UploadFromStreamAsync(stream, overwrite ? null : AccessCondition.GenerateIfNotExistsCondition(), null, null, ct);
            }
            catch (StorageException ex) when (ex.RequestInformation.HttpStatusCode == 409)
            {
                throw new AssetAlreadyExistsException(fileName, ex);
            }
        }

        public Task DeleteAsync(string fileName)
        {
            var name = GetFileName(fileName, nameof(fileName));

            var blob = blobContainer.GetBlockBlobReference(name);

            return blob.DeleteIfExistsAsync();
        }

        private static string GetFileName(string fileName, string parameterName)
        {
            Guard.NotNullOrEmpty(fileName, parameterName);

            return fileName.Replace("\\", "/");
        }
    }
}
