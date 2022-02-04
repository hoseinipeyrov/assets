// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public async Task<(IActionResult Result, AssetTusFile? File)> InvokeAsync(HttpContext httpContext, string baseUrl)
        {
            var customContext = new DefaultHttpContext(httpContext.Features);

            // override the body for error messages from TUS middleware.
            customContext.Response.Body = new MemoryStream();

            customContext.Request.Method = httpContext.Request.Method;
            customContext.Request.Body = httpContext.Request.Body;

            foreach (var (key, value) in httpContext.Request.Headers)
            {
                customContext.Request.Headers[key] = value;
            }

            httpContext.Items["TUS_BASEURL"] = baseUrl;

            await middleware.Invoke(customContext);

            var file = httpContext.Items["TUS_FILE"] as AssetTusFile;

            return (new TusResult(customContext.Response), file);
        }
    }
}
