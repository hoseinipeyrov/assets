// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Squidex.Assets
{
    public sealed class NoopActionResult : IActionResult
    {
        public Task ExecuteResultAsync(ActionContext context)
        {
            return Task.CompletedTask;
        }
    }
}
