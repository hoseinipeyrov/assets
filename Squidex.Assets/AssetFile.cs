﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;
using System.Threading.Tasks;
using Squidex.Assets.Internal;

namespace Squidex.Assets
{
    public abstract class AssetFile : IDisposable, IAsyncDisposable
    {
        public string FileName { get; }

        public string MimeType { get; }

        public long FileSize { get; }

        protected AssetFile(string fileName, string mimeType, long fileSize)
        {
            Guard.NotNullOrEmpty(fileName, nameof(fileName));
            Guard.NotNullOrEmpty(mimeType, nameof(mimeType));
            Guard.GreaterEquals(fileSize, 0, nameof(fileSize));

            FileName = fileName;
            FileSize = fileSize;

            MimeType = mimeType;
        }

        public virtual void Dispose()
        {
        }

        public virtual ValueTask DisposeAsync()
        {
            return default;
        }

        public abstract Stream OpenRead();
    }
}
