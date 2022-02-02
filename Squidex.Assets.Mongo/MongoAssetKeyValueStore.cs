// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Squidex.Assets
{
    public sealed class MongoAssetKeyValueStore<T> : IAssetKeyValueStore<T>
    {
        private readonly ReplaceOptions upsert = new ReplaceOptions
        {
            IsUpsert = true
        };
        private readonly IMongoCollection<MongoAssetEntity<T>> collection;

        public MongoAssetKeyValueStore(IMongoDatabase database)
        {
            var collectionName = $"AssetKeyValueStore_{typeof(T).Name}";

            collection = database.GetCollection<MongoAssetEntity<T>>(collectionName);
        }

        public Task InitializeAsync(
            CancellationToken ct = default)
        {
            BsonClassMap.RegisterClassMap<T>(options =>
            {
                options.AutoMap();
                options.SetIgnoreExtraElements(true);
            });

            return collection.Indexes.CreateOneAsync(
                new CreateIndexModel<MongoAssetEntity<T>>(
                    Builders<MongoAssetEntity<T>>.IndexKeys.Ascending(x => x.Expires)),
                cancellationToken: ct);
        }

        public Task DeleteAsync(string key,
            CancellationToken ct = default)
        {
            return collection.DeleteOneAsync(x => x.Key == key, ct);
        }

        public async Task<T> GetAsync(string key,
            CancellationToken ct = default)
        {
            var entity = await collection.Find(x => x.Key == key).FirstOrDefaultAsync(ct);

            return entity != null ? entity.Value : default;
        }

        public async IAsyncEnumerable<(string Key, T Value)> GetExpiredEntriesAsync(DateTimeOffset now,
            [EnumeratorCancellation] CancellationToken ct = default)
        {
            var entities = await collection.Find(x => x.Expires < now).ToCursorAsync(ct);

            while (await entities.MoveNextAsync(ct))
            {
                foreach (var entity in entities.Current)
                {
                    yield return (entity.Key, entity.Value);
                }
            }
        }

        public Task SetAsync(string key, T value, DateTimeOffset expiration,
            CancellationToken ct = default)
        {
            var entity = new MongoAssetEntity<T> { Key = key, Value = value, Expires = expiration };

            return collection.ReplaceOneAsync(x => x.Key == key, entity, upsert, ct);
        }
    }
}
