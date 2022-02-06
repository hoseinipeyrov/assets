// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

namespace Squidex.Assets
{
    public sealed class DelegatingProgressHandler : IProgressHandler
    {
        internal static readonly DelegatingProgressHandler Instance = new DelegatingProgressHandler();

        public Func<UploadCompletedEvent, CancellationToken, Task>? OnCompletedAsync { get; set; }

        public Func<UploadExceptionEvent, CancellationToken, Task>? OnFailedAsync { get; set; }

        public Func<UploadProgressEvent, CancellationToken, Task>? OnProgressAsync { get; set; }

        async Task IProgressHandler.OnCompletedAsync(UploadCompletedEvent @event,
            CancellationToken ct)
        {
            if (OnCompletedAsync != null)
            {
                await OnCompletedAsync(@event, ct);
            }
        }

        async Task IProgressHandler.OnFailedAsync(UploadExceptionEvent @event,
            CancellationToken ct)
        {
            if (OnFailedAsync != null)
            {
                await OnFailedAsync(@event, ct);
            }
        }

        async Task IProgressHandler.OnProgressAsync(UploadProgressEvent @event,
            CancellationToken ct)
        {
            if (OnProgressAsync != null)
            {
                await OnProgressAsync(@event, ct);
            }
        }
    }
}
