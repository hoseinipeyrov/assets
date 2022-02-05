// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Threading.Tasks;

namespace Squidex.Assets
{
    public sealed class DelegatingProgressHandler : IProgressHandler
    {
        public Func<UploadCompletedEvent, Task>? OnCompletedAsync { get; set; }

        public Func<UploadExceptionEvent, Task>? OnFailedAsync { get; set; }

        public Func<UploadProgressEvent, Task>? OnProgressAsync { get; set; }

        async Task IProgressHandler.OnCompletedAsync(UploadCompletedEvent @event)
        {
            if (OnCompletedAsync != null)
            {
                await OnCompletedAsync(@event);
            }
        }

        async Task IProgressHandler.OnFailedAsync(UploadExceptionEvent @event)
        {
            if (OnFailedAsync != null)
            {
                await OnFailedAsync(@event);
            }
        }

        async Task IProgressHandler.OnProgressAsync(UploadProgressEvent @event)
        {
            if (OnProgressAsync != null)
            {
                await OnProgressAsync(@event);
            }
        }
    }
}
