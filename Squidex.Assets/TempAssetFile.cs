// ==========================================================================
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

        public TempAssetFile(AssetFile source)
            : this(source.FileName, source.MimeType, source.FileSize)
        {
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
            stream.Position = 0;

            return stream;
        }

        public override Stream OpenRead()
        {
            stream.Position = 0;

            return stream;
        }
    }
}
