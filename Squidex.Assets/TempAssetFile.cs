﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.IO;
using System.Threading.Tasks;

namespace Squidex.Assets
{
    public sealed class TempAssetFile : AssetFile
    {
        private readonly Stream stream;

        public static TempAssetFile Create(AssetFile source)
        {
            return new TempAssetFile(source.FileName, source.MimeType, source.FileSize);
        }

        public TempAssetFile(string fileName, string mimeType, long fileSize)
            : base(fileName, mimeType, fileSize)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            stream = new FileStream(tempPath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None, 4096,
                FileOptions.DeleteOnClose);
        }

        public override void Dispose()
        {
            stream.Dispose();
        }

        public override ValueTask DisposeAsync()
        {
            return stream.DisposeAsync();
        }

        public Stream OpenWrite()
        {
            return new NonDisposingStream(stream);
        }

        public override Stream OpenRead()
        {
            return new NonDisposingStream(stream);
        }

        private sealed class NonDisposingStream : DelegatingStream
        {
            public NonDisposingStream(Stream inner)
                : base(inner)
            {
                inner.Position = 0;
            }

            public override void Close()
            {
                Flush();
            }

            protected override void Dispose(bool disposing)
            {
                Flush();
            }

            public override async ValueTask DisposeAsync()
            {
                await FlushAsync();
            }
        }
    }
}
