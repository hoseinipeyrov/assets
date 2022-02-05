// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.IO;
using HeyRed.Mime;

#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

namespace Squidex.Assets
{
    public sealed record UploadFile(Stream Stream, string FileName, string MimeType)
    {
        public static UploadFile FromFile(FileInfo fileInfo, string? mimeType = null)
        {
            if (string.IsNullOrEmpty(mimeType))
            {
                mimeType = MimeTypesMap.GetMimeType(fileInfo.Name);
            }

            return new UploadFile(fileInfo.OpenRead(), fileInfo.Name, mimeType);
        }

        public static UploadFile FromPath(string path, string? mimeType = null)
        {
            return FromFile(new FileInfo(path), mimeType);
        }
    }
}
