﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Net.Http.Headers;
using Squidex.Assets.Internal;

namespace Squidex.Assets
{
    public static class UploadHttpClientExtension
    {
        private static readonly HttpMethod Patch = new HttpMethod("PATCH");

        private static class TusHeaders
        {
            public const string ContentType = "application/offset+octet-stream";
            public const string TusResumable = "Tus-Resumable";
            public const string TusResumableValue = "1.0.0";
            public const string UploadOffset = "Upload-Offset";
            public const string UploadLength = "Upload-Length";
            public const string UploadMetadata = "Upload-Metadata";
        }

        public static Task UploadWithProgressAsync(this HttpClient httpClient, string url, UploadFile file, UploadOptions options = default,
            CancellationToken ct = default)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var uri = new Uri(url, UriKind.RelativeOrAbsolute);

            return httpClient.UploadWithProgressAsync(uri, file, options, ct);
        }

        public static async Task UploadWithProgressAsync(this HttpClient httpClient, Uri uri, UploadFile file, UploadOptions options = default,
            CancellationToken ct = default)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            if (file == null)
            {
                throw new ArgumentNullException(nameof(file));
            }

            var handler = options.ProgressHandler ?? DelegatingProgressHandler.Instance;

            try
            {
                var fileId = options.FileId;
                var totalProgress = 0;
                var totalBytes = file.Stream.Length;
                var bytesWritten = 0L;

                if (!string.IsNullOrWhiteSpace(fileId))
                {
                    bytesWritten = await httpClient.GetSizeAsync(GetFileIdUrl(uri, fileId!), ct);

                    if (bytesWritten > 0)
                    {
                        file.Stream.Seek(bytesWritten, SeekOrigin.Begin);
                    }
                }

                if (bytesWritten == 0 || string.IsNullOrWhiteSpace(fileId))
                {
                    fileId = await httpClient.CreateAsync(uri, file, options, ct);
                }

                var url = GetFileIdUrl(uri, fileId!);

                var content = new ProgressableStreamContent(file.Stream, async bytes =>
                {
                    try
                    {
                        bytesWritten = bytes;

                        if (bytesWritten == totalBytes)
                        {
                            await handler.OnProgressAsync(new UploadProgressEvent(fileId!, 100, totalBytes, totalBytes), ct);
                            return;
                        }

                        var newProgress = (int)Math.Floor(100 * (double)bytesWritten / totalBytes);

                        if (newProgress != totalProgress)
                        {
                            totalProgress = newProgress;

                            await handler.OnProgressAsync(new UploadProgressEvent(fileId!, totalProgress, bytes, totalBytes), ct);
                        }
                    }
                    catch
                    {
                        return;
                    }
                });

                content.Headers.TryAddWithoutValidation(HeaderNames.ContentType, TusHeaders.ContentType);

                var request =
                    new HttpRequestMessage(Patch, url) { Content = content }
                        .WithDefaultHeaders()
                        .WithHeader(TusHeaders.UploadOffset, bytesWritten);

                var response = await httpClient.SendAsync(request, ct);

                response.EnsureSuccessStatusCode();

                if (bytesWritten == totalBytes)
                {
                    try
                    {
                        await handler.OnCompletedAsync(new UploadCompletedEvent(fileId!, response), ct);
                    }
                    finally
                    {
                        await httpClient.TerminateAsync(url, default);
                    }
                }
            }
            catch (Exception ex)
            {
                await handler.OnFailedAsync(new UploadExceptionEvent(file.FileName, ex), ct);
            }
        }

        private static async Task<string> CreateAsync(this HttpClient httpClient, Uri uri, UploadFile file, UploadOptions options,
            CancellationToken ct)
        {
            var metadata = new StringBuilder();
            metadata.Append("FileName ");
            metadata.Append(file.FileName.ToBase64());
            metadata.Append(',');
            metadata.Append("MimeType ");
            metadata.Append(file.ContentType.ToBase64());

            if (options.Metadata != null)
            {
                foreach (var kvp in options.Metadata.Where(x => x.Value != null))
                {
                    metadata.Append(',');
                    metadata.Append(kvp.Key);
                    metadata.Append(' ');
                    metadata.Append(kvp.Value.ToBase64());
                }
            }

            var request =
                new HttpRequestMessage(HttpMethod.Post, uri)
                    .WithDefaultHeaders()
                    .WithHeader(TusHeaders.UploadMetadata, metadata.ToString())
                    .WithHeader(TusHeaders.UploadLength, file.Stream.Length);

            var response = await httpClient.SendAsync(request, ct);

            response.EnsureSuccessStatusCode();
            response.CheckTusResponse();

            if (response.StatusCode != HttpStatusCode.Created)
            {
                throw new HttpRequestException($"Server did not answer with status code 201. Received: {(int)response.StatusCode}.");
            }

            if (!response.Headers.TryGetValues(HeaderNames.Location, out var location) || !location.Any())
            {
                throw new HttpRequestException($"Server did not answer location.");
            }

            var locationValue = location.First().Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

#pragma warning disable MA0098 // Use indexer instead of LINQ methods
            return locationValue.Last();
#pragma warning restore MA0098 // Use indexer instead of LINQ methods
        }

        public static Task<bool> DeleteUploadAsync(this HttpClient httpClient, string url, string fileId,
          CancellationToken ct = default)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            var uri = new Uri(url, UriKind.RelativeOrAbsolute);

            return httpClient.DeleteUploadAsync(uri, fileId, ct);
        }

        public static async Task<bool> DeleteUploadAsync(this HttpClient httpClient, Uri uri, string fileId,
            CancellationToken ct = default)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            return await TerminateAsync(httpClient, GetFileIdUrl(uri, fileId), ct);
        }

        private static async Task<bool> TerminateAsync(this HttpClient httpClient, string url,
            CancellationToken ct)
        {
            var request =
                new HttpRequestMessage(HttpMethod.Delete, url)
                    .WithDefaultHeaders();

            try
            {
                var response = await httpClient.SendAsync(request, ct);

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<long> GetSizeAsync(this HttpClient httpClient, string url,
            CancellationToken ct)
        {
            var request =
                new HttpRequestMessage(HttpMethod.Head, url)
                    .WithDefaultHeaders();

            var response = await httpClient.SendAsync(request, ct);

            response.CheckTusResponse();

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return 0;
            }

            response.EnsureSuccessStatusCode();

            if (!response.Headers.TryGetValues(TusHeaders.UploadOffset, out var offset) || !offset.Any())
            {
                return 0;
            }

            if (!long.TryParse(offset.First(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedNumber))
            {
                return 0;
            }

            return parsedNumber;
        }

        private static void CheckTusResponse(this HttpResponseMessage response)
        {
            if (!response.Headers.TryGetValues(TusHeaders.TusResumable, out var resumable) || resumable.FirstOrDefault() != TusHeaders.TusResumableValue)
            {
                throw new InvalidOperationException("TUS is not supported for this endpoint.");
            }
        }

        private static HttpRequestMessage WithDefaultHeaders(this HttpRequestMessage message)
        {
            message.Headers.TryAddWithoutValidation(TusHeaders.TusResumable, TusHeaders.TusResumableValue);

            return message;
        }

        private static HttpRequestMessage WithHeader(this HttpRequestMessage message, string key, object value)
        {
            message.Headers.TryAddWithoutValidation(key, Convert.ToString(value, CultureInfo.InvariantCulture));

            return message;
        }

        private static HttpRequestMessage WithHeader(this HttpRequestMessage message, string key, string value)
        {
            message.Headers.TryAddWithoutValidation(key, value);

            return message;
        }

        private static string ToBase64(this string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);

            return Convert.ToBase64String(bytes);
        }

        private static string GetFileIdUrl(Uri uri, string id)
        {
            var url = uri.ToString();

#if NETCOREAPP3_1_OR_GREATER
            if (!url.EndsWith('/'))
#else
            if (!url.EndsWith("/", StringComparison.Ordinal))
#endif
            {
                url += '/';
            }

            url += Uri.EscapeDataString(id);

            return url;
        }
    }
}
