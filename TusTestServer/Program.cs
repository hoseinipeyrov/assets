// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Squidex.Assets;
using tusdotnet;
using tusdotnet.Interfaces;
using tusdotnet.Models;
using tusdotnet.Models.Configuration;
using TutTestServer;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
});

var mongoClient = new MongoClient("mongodb://localhost");
var mongoDatabase = mongoClient.GetDatabase("TusTest");

var gridFSBucket = new GridFSBucket<string>(mongoDatabase, new GridFSBucketOptions
{
    BucketName = "fs"
});

builder.Services.AddSingleton<IMongoDatabase>(
    mongoDatabase);
builder.Services.AddSingleton<IHostedService,
    Initializer>();
builder.Services.AddSingleton<ITusStore,
    AssetTusStore>();
builder.Services.AddSingleton<IAssetStore>(
    new MongoGridFsAssetStore(gridFSBucket));
builder.Services.AddSingleton<IAssetKeyValueStore<TusMetadata>,
    MongoAssetKeyValueStore<TusMetadata>>();

var app = builder.Build();

app.UseTus(httpContext => new DefaultTusConfiguration
{
    Store = httpContext.RequestServices.GetRequiredService<ITusStore>(),
    UrlPath = "/files/",
    Events = new Events
    {
        OnFileCompleteAsync = async eventContext =>
        {
            var fileObject = (AssetFile)(await eventContext.GetFileAsync());

            await using var fileStream = fileObject.OpenRead();

            var name = fileObject.FileName;

            if (string.IsNullOrWhiteSpace(name))
            {
                name = Guid.NewGuid().ToString();
            }

            Directory.CreateDirectory("uploads");

            await using (var stream = new FileStream($"uploads/{name}", FileMode.CreateNew))
            {
                await fileStream.CopyToAsync(stream, eventContext.CancellationToken);
            }
        }
    }
});

app.UseStaticFiles();

app.Run();
