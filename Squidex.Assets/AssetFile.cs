// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;

namespace Squidex.Assets
{
    public abstract class AssetFile : IDisposable
    {
        public string FileName { get; }

        public string MimeType { get; }

        public long FileSize { get; }

        protected AssetFile(string fileName, string mimeType, long fileSize)
        {
            AssetsGuard.NotNullOrEmpty(fileName, nameof(fileName));
            AssetsGuard.NotNullOrEmpty(mimeType, nameof(mimeType));
            AssetsGuard.GreaterEquals(fileSize, 0, nameof(fileSize));

            FileName = fileName;
            FileSize = fileSize;

            MimeType = mimeType;
        }

        public virtual void Dispose()
        {
        }

        public abstract Stream OpenRead();
    }
}
