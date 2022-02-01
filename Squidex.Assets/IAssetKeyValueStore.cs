// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Squidex.Assets
{
    public interface IKeyValueStore
    {
        Task<T> GetAsync<T>(string id,
            CancellationToken ct = default);

        Task SetAsync<T>(string id, T value, DateTimeOffset expiration,
            CancellationToken ct = default);

        Task DeleteAsync(string id,
            CancellationToken ct = default);

        IAsyncEnumerable<(string Key, T Value)> GetExpiredEntriesAsync<T>(DateTimeOffset now,
            CancellationToken ct = default);
    }
}
