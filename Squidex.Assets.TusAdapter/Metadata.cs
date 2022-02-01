// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;

namespace Squidex.Assets
{
    public sealed class Metadata
    {
        public DateTimeOffset? Expiration { get; set; }

        public string Id { get; set; }

        public long? UploadLength { get; set; }

        public string UploadMetadata { get; set; }

        public long BytesWritten { get; set; }

        public int Parts { get; set; }

        public bool Created { get; set; }
    }
}
