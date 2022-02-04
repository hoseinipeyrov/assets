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

namespace Squidex.Assets
{
    internal sealed class TusResult : IActionResult
    {
        private readonly HttpResponse response;

        public TusResult(HttpResponse response)
        {
            this.response = response;
        }

        public async Task ExecuteResultAsync(ActionContext context)
        {
            if (response.StatusCode > 0)
            {
                context.HttpContext.Response.StatusCode = response.StatusCode;
            }

            foreach (var (key, value) in response.Headers)
            {
                context.HttpContext.Response.Headers[key] = value;
            }

            if (response.Body.Length > 0)
            {
                response.Body.Seek(0, SeekOrigin.Begin);

                await response.Body.CopyToAsync(context.HttpContext.Response.Body, context.HttpContext.RequestAborted);
            }
        }
    }
}
