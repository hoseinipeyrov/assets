// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Squidex.Assets.Remote
{
    public sealed class RemoteThumbnailGenerator : IAssetThumbnailGenerator
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IAssetThumbnailGenerator inner;

        public RemoteThumbnailGenerator(IHttpClientFactory httpClientFactory, IAssetThumbnailGenerator inner)
        {
            this.httpClientFactory = httpClientFactory;

            this.inner = inner;
        }

        public async Task CreateThumbnailAsync(Stream source, string mimeType, Stream destination, ResizeOptions options,
            CancellationToken ct = default)
        {
            using (var httpClient = httpClientFactory.CreateClient("Resize"))
            {
                var query = BuildQueryString(options);

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"/resize{query}")
                {
                    Content = new StreamContent(source)
                };

                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

                var response = await httpClient.SendAsync(requestMessage, ct);

                response.EnsureSuccessStatusCode();

                await response.Content.CopyToAsync(destination);
            }
        }

        public async Task FixOrientationAsync(Stream source, string mimeType, Stream destination,
            CancellationToken ct = default)
        {
            using (var httpClient = httpClientFactory.CreateClient("Resize"))
            {
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, "/orient")
                {
                    Content = new StreamContent(source)
                };

                requestMessage.Content.Headers.ContentType = new MediaTypeHeaderValue(mimeType);

                var response = await httpClient.SendAsync(requestMessage, ct);

                response.EnsureSuccessStatusCode();

                await response.Content.CopyToAsync(destination);
            }
        }

        public Task<ImageInfo?> GetImageInfoAsync(Stream source, string mimeType,
            CancellationToken ct = default)
        {
            return inner.GetImageInfoAsync(source, mimeType, ct);
        }

        private static string BuildQueryString(ResizeOptions options)
        {
            var sb = new StringBuilder();

            foreach (var (key, value) in options.ToParameters())
            {
                if (sb.Length > 0)
                {
                    sb.Append('&');
                }
                else
                {
                    sb.Append('?');
                }

                sb.Append(key);
                sb.Append('=');
                sb.Append(Uri.EscapeDataString(value));
            }

            return sb.ToString();
        }
    }
}
