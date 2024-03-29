﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public sealed class AmazonS3AssetOptions
    {
        public string ServiceUrl { get; set; }

        public string RegionName { get; set; }

        public string Bucket { get; set; }

        public string BucketFolder { get; set; }

        public string AccessKey { get; set; }

        public string SecretKey { get; set; }

        public bool ForcePathStyle { get; set; }
    }
}
