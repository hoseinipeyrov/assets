// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.IO;
using Microsoft.Extensions.Configuration;

namespace Squidex.Assets
{
    public static class TestHelpers
    {
        public static IConfiguration Configuration { get; }

        static TestHelpers()
        {
            var basePath = Path.GetFullPath("../../../");

            Configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("appsettings.Development.json", true)
                .AddEnvironmentVariables()
                .Build();
        }
    }
}
