// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Mvc;
using Squidex.Assets;

namespace TusTestServer.Controller
{
    public class TusController : ControllerBase
    {
        private readonly AssetTusRunner runner;

        public TusController(AssetTusRunner runner)
        {
            this.runner = runner;
        }

        [Route("files/controller/{**catchAll}")]
        public async Task<IActionResult> Tus()
        {
            var file = await runner.InvokeAsync(HttpContext, Url.Action(null, new { catchAll = (string?)null }));

            if (file != null)
            {
                await using var fileStream = file.OpenRead();

                var name = file.FileName;

                if (string.IsNullOrWhiteSpace(name))
                {
                    name = Guid.NewGuid().ToString();
                }

                Directory.CreateDirectory("uploads");

                await using (var stream = new FileStream($"uploads/{name}", FileMode.Create))
                {
                    await fileStream.CopyToAsync(stream, HttpContext.RequestAborted);
                }

                return Ok(new { json = "Test" });
            }

            return new NoopActionResult();
        }
    }
}
