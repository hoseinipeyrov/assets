// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using tusdotnet;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;

namespace Squidex.Assets
{
    public sealed class AssetTusRunner
    {
        private readonly TusCoreMiddleware middleware;

        public AssetTusRunner(AssetTusStore tusStore)
        {
            var events = new Events
            {
                OnFileCompleteAsync = async eventContext =>
                {
                    var file = await eventContext.GetFileAsync();

                    if (file is AssetTusFile tusFile)
                    {
                        eventContext.HttpContext.Items["TUS_FILE"] = file;
                    }
                }
            };

            middleware = new TusCoreMiddleware(_ => Task.CompletedTask, ctx => Task.FromResult(new DefaultTusConfiguration
            {
                Store = tusStore,
                MaxAllowedUploadSizeInBytes = null,
                MaxAllowedUploadSizeInBytesLong = null,
                Events = events,
                UrlPath = ctx.Items["TUS_BASEURL"]!.ToString()
            }));
        }

        public async Task<AssetTusFile> InvokeAsync(HttpContext httpContext, string baseUrl)
        {
            httpContext.Items["TUS_BASEURL"] = baseUrl;

            await middleware.Invoke(httpContext);

            return httpContext.Items["TUS_FILE"] as AssetTusFile;
        }
    }
}
