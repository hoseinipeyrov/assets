// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using Squidex.Assets.ImageMagick;
using Squidex.Assets.ImageSharp;
using Squidex.Log;

namespace Squidex.Assets.ResizeService
{
    public sealed class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingletonAs(_ => JsonLogWriterFactory.Readable())
                .As<IRootWriterFactory>();

            services.AddHealthChecks();
            services.AddDefaultForwardRules();
            services.AddDefaultWebServices(configuration);

            services.AddSingletonAs(c => new CompositeThumbnailGenerator(new IAssetThumbnailGenerator[]
            {
                new ImageSharpThumbnailGenerator(),
                new ImageMagickThumbnailGenerator()
            })).As<IAssetThumbnailGenerator>();
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseDefaultPathBase();
            app.UseDefaultForwardRules();

            app.UseRouting();
            app.UseHealthChecks("/healthz");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost("/resize", async context =>
                {
                    var thumbnailGenerator = context.RequestServices.GetRequiredService<IAssetThumbnailGenerator>();

                    var options = ResizeOptions.Parse(context.Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString()));

                    await thumbnailGenerator.CreateThumbnailAsync(
                        context.Request.Body,
                        context.Request.ContentType ?? "image/png",
                        context.Response.Body, options,
                        context.RequestAborted);
                });

                endpoints.MapPost("/orient", async context =>
                {
                    var thumbnailGenerator = context.RequestServices.GetRequiredService<IAssetThumbnailGenerator>();

                    await thumbnailGenerator.FixOrientationAsync(
                        context.Request.Body,
                        context.Request.ContentType ?? "image/png",
                        context.Response.Body,
                        context.RequestAborted);
                });
            });
        }
    }
}
