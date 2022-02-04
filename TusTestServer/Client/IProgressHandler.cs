// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

namespace TusTestServer.Client
{
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter
    public abstract record UploadEvent(string FileId);

    public sealed record UploadProgressEvent(string FileId, long Progress, long BytesWritten, long BytesTotal)
        : UploadEvent(FileId);

    public sealed record UploadCompletedEvent(string FileId, HttpResponseMessage Response)
        : UploadEvent(FileId);

    public sealed record UploadExceptionEvent(string FileId, Exception Exception)
        : UploadEvent(FileId);
#pragma warning restore SA1313 // Parameter names should begin with lower-case letter

    public interface IProgressHandler
    {
        Task OnProgressAsync(UploadProgressEvent @event);

        Task OnCompletedAsync(UploadCompletedEvent @event);

        Task OnFailedAsync(UploadExceptionEvent @event);
    }
}
