﻿// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Microsoft.AspNetCore.Mvc;
using Squidex.Assets;
using TusTestServer.Client;

namespace TusTestServer.Controller
{
    public class TusController : ControllerBase
    {
        private readonly AssetTusRunner runner;
        private string? fileId;

        public TusController(AssetTusRunner runner)
        {
            this.runner = runner;
        }

        [Route("/upload")]
        public async Task UploadAsync()
        {
            using (var httpClient = new HttpClient())
            {
                var file = new FileInfo("wwwroot/LargeImage.jpg");

                var cts = new CancellationTokenSource();

                await httpClient.UploadWithProgressAsync(new Uri("http://localhost:5010/files/controller"), file, null, new DelegatingProgressHandler
                {
                    OnProgressAsync = @event =>
                    {
                        fileId = @event.FileId;

                        if (@event.Progress > 50 && !cts.IsCancellationRequested)
                        {
                            cts.Cancel();
                        }

                        return Task.CompletedTask;
                    }
                }, cts.Token);

                await Task.Delay(1000, default);

                if (cts.IsCancellationRequested)
                {
                    await httpClient.UploadWithProgressAsync(new Uri("http://localhost:5010/files/controller"), file, fileId, ct: default);
                }
            }
        }

        [Route("files/controller/{**catchAll}")]
        public async Task<IActionResult> Tus()
        {
            var (result, file) = await runner.InvokeAsync(HttpContext, Url.Action(null, new { catchAll = (string?)null })!);

            if (file == null)
            {
                return result;
            }

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
    }
}