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
        private const string TusFile = "TUS_FILE";
        private const string TusUrl = "TUS_FILE";
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
                        eventContext.HttpContext.Items[TusFile] = file;
                    }
                }
            };

            middleware = new TusCoreMiddleware(_ => Task.CompletedTask, ctx => Task.FromResult(new DefaultTusConfiguration
            {
                Store = tusStore,

                // Reuse the events to avoid allocations.
                Events = events,

                // Get the url from the controller that is temporarily stored in the items.
                UrlPath = ctx.Items[TusUrl]!.ToString()
            }));
        }

        public async Task<(IActionResult Result, AssetTusFile? File)> InvokeAsync(HttpContext httpContext, string baseUrl)
        {
            var customContext = BuildCustomContext(httpContext, baseUrl);

            await middleware.Invoke(customContext);

            var file = customContext.Items[TusFile] as AssetTusFile;

            return (new TusResult(customContext.Response), file);
        }

        private static DefaultHttpContext BuildCustomContext(HttpContext httpContext, string baseUrl)
        {
            // Features transport a lot of logic, such as items and pipe readers and so on.
            var customContext = new DefaultHttpContext(httpContext.Features);

            // Override the body for error messages from TUS middleware. They are usually small so buffering here is okay.
            customContext.Response.Body = new MemoryStream();

            customContext.Request.Method = httpContext.Request.Method;
            customContext.Request.Body = httpContext.Request.Body;
            customContext.Items[TusUrl] = baseUrl;

            foreach (var (key, value) in httpContext.Request.Headers)
            {
                customContext.Request.Headers[key] = value;
            }

            return customContext;
        }
    }
}
