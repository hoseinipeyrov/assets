// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Net;

namespace TusTestServer.Client
{
    internal sealed class ProgressableStreamContent : HttpContent
    {
        private readonly Stream content;
        private readonly int uploadBufferSize;
        private readonly Func<long, Task> uploadProgress;

        public ProgressableStreamContent(Stream content, Func<long, Task> uploadProgress)
            : this(content, 4096, uploadProgress)
        {
        }

        public ProgressableStreamContent(Stream content, int uploadBufferSize, Func<long, Task> uploadProgress)
        {
            this.content = content;
            this.uploadBufferSize = uploadBufferSize;
            this.uploadProgress = uploadProgress;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            return SerializeToStreamAsync(stream, default);
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context,
            CancellationToken cancellationToken)
        {
            return SerializeToStreamAsync(stream, cancellationToken);
        }

        private async Task SerializeToStreamAsync(Stream stream,
            CancellationToken ct)
        {
            var buffer = new byte[uploadBufferSize].AsMemory();

            while (true)
            {
                var bytesRead = await content.ReadAsync(buffer, ct);

                if (bytesRead <= 0)
                {
                    break;
                }

                await stream.WriteAsync(buffer[..bytesRead], ct);

                await uploadProgress(content.Position);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = content.Length - content.Position;

            return true;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                content.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
