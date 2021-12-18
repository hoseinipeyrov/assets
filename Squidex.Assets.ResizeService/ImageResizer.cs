// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Log;

namespace Squidex.Assets.ResizeService
{
    public sealed class ImageResizer
    {
        private readonly IAssetThumbnailGenerator assetThumbnailGenerator;

        public ImageResizer(IAssetThumbnailGenerator assetThumbnailGenerator)
        {
            this.assetThumbnailGenerator = assetThumbnailGenerator;
        }

        public void Map(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapPost("/resize", async context =>
            {
                await ResizeAsync(context);
            });

            endpoints.MapPost("/orient", async context =>
            {
                await OrientAsync(context);
            });
        }

        private async Task OrientAsync(HttpContext context)
        {
            await using var tempStream = GetTempStream();

            await context.Request.Body.CopyToAsync(tempStream, context.RequestAborted);
            tempStream.Position = 0;

            try
            {
                await assetThumbnailGenerator.FixOrientationAsync(
                    tempStream,
                    context.Request.ContentType ?? "image/png",
                    context.Response.Body,
                    context.RequestAborted);
            }
            catch (Exception ex)
            {
                var log = context.RequestServices.GetRequiredService<ISemanticLog>();

                log.LogError(ex, w => w
                    .WriteProperty("action", "Resize")
                    .WriteProperty("status", "Failed"));

                context.Response.StatusCode = 400;
            }
        }

        private async Task ResizeAsync(HttpContext context)
        {
            await using var tempStream = GetTempStream();

            await context.Request.Body.CopyToAsync(tempStream, context.RequestAborted);
            tempStream.Position = 0;

            try
            {
                var options = ResizeOptions.Parse(context.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()));

                await assetThumbnailGenerator.CreateThumbnailAsync(
                    tempStream,
                    context.Request.ContentType ?? "image/png",
                    context.Response.Body, options,
                    context.RequestAborted);
            }
            catch (Exception ex)
            {
                var log = context.RequestServices.GetRequiredService<ISemanticLog>();

                log.LogError(ex, w => w
                    .WriteProperty("action", "Resize")
                    .WriteProperty("status", "Failed"));

                context.Response.StatusCode = 400;
            }
        }

        private Stream GetTempStream()
        {
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetTempFileName());

            var stream = new FileStream(tempPath,
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None, 4096,
                FileOptions.DeleteOnClose);

            return stream;
        }
    }
}
