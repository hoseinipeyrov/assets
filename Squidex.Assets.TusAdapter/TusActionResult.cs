// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Squidex.Assets
{
    internal sealed class TusActionResult : IActionResult
    {
        private readonly HttpResponse response;

        public int StatusCode => response.StatusCode;

        public IHeaderDictionary Headers => response.Headers;

        public Stream Body => response.Body;

        public TusActionResult(HttpResponse response)
        {
            this.response = response;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (StatusCode > 0)
            {
                context.HttpContext.Response.StatusCode = StatusCode;
            }

            foreach (var (key, value) in Headers)
            {
                context.HttpContext.Response.Headers[key] = value;
            }

            if (Body.Length > 0)
            {
                Body.Seek(0, SeekOrigin.Begin);

                await Body.CopyToAsync(context.HttpContext.Response.Body, context.HttpContext.RequestAborted);
            }
        }
    }
}
