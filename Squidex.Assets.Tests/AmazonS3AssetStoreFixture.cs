// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public sealed class AmazonS3AssetStoreFixture
    {
        public AmazonS3AssetStore AssetStore { get; }

        public AmazonS3AssetStoreFixture()
        {
            // From: https://console.aws.amazon.com/iam/home?region=eu-central-1#/users/s3?section=security_credentials
            AssetStore = new AmazonS3AssetStore(new AmazonS3Options
            {
                AccessKey = "AKIAYR4IRKRWOZHXU5TB",
                Bucket = "squidex-test",
                BucketFolder = "squidex-assets",
                ForcePathStyle = false,
                RegionName = "eu-central-1",
                SecretKey = "TbChpTIbBjTJfB6R/BNepIBF6S5g/vZedeH3057s",
                ServiceUrl = null
            });
            AssetStore.InitializeAsync().Wait();
        }
    }
}
