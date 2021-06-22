// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Xunit;

namespace Squidex.Assets
{
    public abstract class AssetStoreTests<T> where T : IAssetStore
    {
        private readonly MemoryStream assetLarge = CreateFile(4 * 1024 * 1024);
        private readonly MemoryStream assetSmall = CreateFile(4);
        private readonly Lazy<T> sut;

        protected T Sut
        {
            get { return sut.Value; }
        }

        protected string FileName { get; } = Guid.NewGuid().ToString();

        protected virtual bool CanUploadStreamsWithoutLength => true;

        protected AssetStoreTests()
        {
            sut = new Lazy<T>(CreateStore);
        }

        public abstract T CreateStore();

        public static IEnumerable<object[]> FolderCases()
        {
            yield return new object[] { false };
            yield return new object[] { true };
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public virtual async Task Should_throw_exception_if_asset_to_get_size_is_not_found(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.GetSizeAsync(path));
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public virtual async Task Should_throw_exception_if_asset_to_download_is_not_found(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.DownloadAsync(path, new MemoryStream()));
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_throw_exception_if_asset_to_copy_is_not_found(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Assert.ThrowsAsync<AssetNotFoundException>(() => Sut.CopyAsync(path, Guid.NewGuid().ToString()));
        }

        [Fact]
        public async Task Should_throw_exception_if_stream_to_download_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Sut.DownloadAsync("File", null!));
        }

        [Fact]
        public async Task Should_throw_exception_if_stream_to_upload_is_null()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => Sut.UploadAsync("File", null!));
        }

        [Fact]
        public async Task Should_throw_exception_if_source_file_name_to_copy_is_empty()
        {
            await CheckEmpty(v => Sut.CopyAsync(v, "Target"));
        }

        [Fact]
        public async Task Should_throw_exception_if_target_file_name_to_copy_is_empty()
        {
            await CheckEmpty(v => Sut.CopyAsync("Source", v));
        }

        [Fact]
        public async Task Should_throw_exception_if_file_name_to_delete_is_empty()
        {
            await CheckEmpty(v => Sut.DeleteAsync(v));
        }

        [Fact]
        public async Task Should_throw_exception_if_file_name_to_download_is_empty()
        {
            await CheckEmpty(v => Sut.DownloadAsync(v, new MemoryStream()));
        }

        [Fact]
        public async Task Should_throw_exception_if_file_name_to_upload_is_empty()
        {
            await CheckEmpty(v => Sut.UploadAsync(v, new MemoryStream()));
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_upload_compressed_file(bool withFolder)
        {
            var path = GetPath(withFolder);

            if (!CanUploadStreamsWithoutLength)
            {
                return;
            }

            var source = CreateDeflateStream(20_000);

            await Sut.UploadAsync(path, source);

            var readData = new MemoryStream();

            await Sut.DownloadAsync(path, readData);

            Assert.True(readData.Length > 0);
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_write_and_read_file(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Sut.UploadAsync(path, assetSmall);

            var readData = new MemoryStream();

            await Sut.DownloadAsync(path, readData);

            Assert.Equal(assetSmall.ToArray(), readData.ToArray());
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_write_and_read_large_file(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Sut.UploadAsync(path, assetLarge);

            var readData = new MemoryStream();

            await Sut.DownloadAsync(path, readData);

            Assert.Equal(assetLarge.ToArray(), readData.ToArray());
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_write_and_read_file_with_range(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Sut.UploadAsync(path, assetSmall, true);

            var readData = new MemoryStream();

            await Sut.DownloadAsync(path, readData, new BytesRange(1, 2));

            Assert.Equal(new Span<byte>(assetSmall.ToArray()).Slice(1, 2).ToArray(), readData.ToArray());
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_copy_and_read_file(bool withFolder)
        {
            var path = GetPath(withFolder);

            var tempFile = Guid.NewGuid().ToString();

            await Sut.UploadAsync(tempFile, assetSmall);
            try
            {
                await Sut.CopyAsync(tempFile, path);

                var readData = new MemoryStream();

                await Sut.DownloadAsync(path, readData);

                Assert.Equal(assetSmall.ToArray(), readData.ToArray());
            }
            finally
            {
                await Sut.DeleteAsync(tempFile);
            }
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_write_and_and_get_size(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Sut.UploadAsync(path, assetSmall, true);

            var size = await Sut.GetSizeAsync(path);

            Assert.Equal(assetSmall.Length, size);
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_write_and_read_file_and_overwrite_non_existing(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Sut.UploadAsync(path, assetSmall, true);

            var readData = new MemoryStream();

            await Sut.DownloadAsync(path, readData);

            Assert.Equal(assetSmall.ToArray(), readData.ToArray());
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_write_and_read_overriding_file(bool withFolder)
        {
            var path = GetPath(withFolder);

            var oldData = new MemoryStream(new byte[] { 0x1, 0x2, 0x3, 0x4, 0x5 });

            await Sut.UploadAsync(path, oldData);
            await Sut.UploadAsync(path, assetSmall, true);

            var readData = new MemoryStream();

            await Sut.DownloadAsync(path, readData);

            Assert.Equal(assetSmall.ToArray(), readData.ToArray());
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_throw_exception_when_file_to_write_already_exists(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Sut.UploadAsync(path, assetSmall);

            await Assert.ThrowsAsync<AssetAlreadyExistsException>(() => Sut.UploadAsync(path, assetSmall));
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_throw_exception_when_target_file_to_copy_to_already_exists(bool withFolder)
        {
            var path = GetPath(withFolder);

            var tempFile = Guid.NewGuid().ToString();

            await Sut.UploadAsync(tempFile, assetSmall);
            await Sut.CopyAsync(tempFile, path);

            await Assert.ThrowsAsync<AssetAlreadyExistsException>(() => Sut.CopyAsync(tempFile, path));
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_ignore_when_deleting_deleted_file(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Sut.UploadAsync(path, assetSmall);
            await Sut.DeleteAsync(path);
            await Sut.DeleteAsync(path);
        }

        [Theory]
        [MemberData(nameof(FolderCases))]
        public async Task Should_ignore_when_deleting_not_existing_file(bool withFolder)
        {
            var path = GetPath(withFolder);

            await Sut.DeleteAsync(path);
        }

        private static async Task CheckEmpty(Func<string, Task> action)
        {
            await Assert.ThrowsAsync<ArgumentNullException>(() => action(null!));
            await Assert.ThrowsAsync<ArgumentException>(() => action(string.Empty));
            await Assert.ThrowsAsync<ArgumentException>(() => action(" "));
        }

        private static MemoryStream CreateFile(int length)
        {
            var memoryStream = new MemoryStream();

            for (var i = 0; i < length; i++)
            {
                memoryStream.WriteByte((byte)i);
            }

            memoryStream.Position = 0;

            return memoryStream;
        }

        private static Stream CreateDeflateStream(int length)
        {
            var memoryStream = new MemoryStream();

            using (var archive1 = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
            {
                using (var file = archive1.CreateEntry("test").Open())
                {
                    var test = CreateFile(length);

                    test.CopyTo(file);
                }
            }

            memoryStream.Position = 0;

            var archive2 = new ZipArchive(memoryStream, ZipArchiveMode.Read);

            return archive2.GetEntry("test").Open();
        }

        private static string GetPath(bool withFolder)
        {
            if (withFolder)
            {
                return $"{Guid.NewGuid()}/{Guid.NewGuid()}";
            }

            return $"{Guid.NewGuid()}";
        }
    }
}
