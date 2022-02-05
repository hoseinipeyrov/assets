// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.Net.Http;
using System.Threading.Tasks;

#pragma warning disable MA0048 // File name must match type name
#pragma warning disable SA1313 // Parameter names should begin with lower-case letter

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

        public UploadProgressEvent(string fileId, int Progress, long BytesWritten, long BytesTotal)
            : base(fileId)
        {
            this.Progress = Progress;
            this.BytesWritten = BytesWritten;
            this.BytesTotal = BytesTotal;
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
        Task OnProgressAsync(UploadProgressEvent @event);

        Task OnCompletedAsync(UploadCompletedEvent @event);

        Task OnFailedAsync(UploadExceptionEvent @event);
    }
}
