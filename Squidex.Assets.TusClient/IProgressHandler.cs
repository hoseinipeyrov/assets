// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

#pragma warning disable MA0048 // File name must match type name

namespace Squidex.Assets
{
    public abstract class UploadEvent
    {
        public string FileId { get; }

        protected UploadEvent(string fileId)
        {
            FileId = fileId;
        }
    }

    public sealed class UploadProgressEvent : UploadEvent
    {
        public int Progress { get; }

        public long BytesWritten { get; }

        public long BytesTotal { get; }

        public UploadProgressEvent(string fileId, int progress, long bytesWritten, long bytesTotal)
            : base(fileId)
        {
            Progress = progress;
            BytesWritten = bytesWritten;
            BytesTotal = bytesTotal;
        }
    }

    public sealed class UploadCompletedEvent : UploadEvent
    {
        public HttpResponseMessage Response { get; }

        public UploadCompletedEvent(string fileId, HttpResponseMessage response)
            : base(fileId)
        {
            Response = response;
        }
    }

    public sealed class UploadExceptionEvent : UploadEvent
    {
        public Exception Exception { get; }

        public UploadExceptionEvent(string fileId, Exception exception)
            : base(fileId)
        {
            Exception = exception;
        }
    }

    public interface IProgressHandler
    {
        Task OnProgressAsync(UploadProgressEvent @event,
            CancellationToken ct);

        Task OnCompletedAsync(UploadCompletedEvent @event,
            CancellationToken ct);

        Task OnFailedAsync(UploadExceptionEvent @event,
            CancellationToken ct);
    }
}
