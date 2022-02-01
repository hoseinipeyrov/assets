// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschraenkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Squidex.Assets
{
    public sealed class CancellableStream : DelegateStream
    {
        private readonly CancellationToken cancellationToken;

        public override long Length
        {
            get => throw new NotSupportedException();
        }

        public override bool CanWrite
        {
            get => false;
        }

        public CancellableStream(Stream innerStream, CancellationToken cancellationToken)
            : base(innerStream)
        {
            this.cancellationToken = cancellationToken;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return -1;
            }

            return base.Read(buffer, offset, count);
        }

        public override int Read(Span<byte> buffer)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return -1;
            }

            return base.Read(buffer);
        }

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult(-1);
            }

            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }

        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<int>(-1);
            }

            return base.ReadAsync(buffer, cancellationToken);
        }

        public override int ReadByte()
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return -1;
            }

            return base.ReadByte();
        }
    }
}
