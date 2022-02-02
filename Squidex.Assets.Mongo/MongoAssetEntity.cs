// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using MongoDB.Bson.Serialization.Attributes;

namespace Squidex.Assets
{
    internal sealed class MongoAssetEntity<T>
    {
        [BsonId]
        public string Key { get; set; }

        public T Value { get; set; }

        public DateTimeOffset Expires { get; set; }
    }
}
