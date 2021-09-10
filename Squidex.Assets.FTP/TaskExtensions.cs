﻿using System.Threading;
using System.Threading.Tasks;

namespace Squidex.Assets
{
    public static class TaskExtensions
    {
        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken)
        {
            if (!cancellationToken.CanBeCanceled)
            {
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<TResult>(cancellationToken);
            }

            return DoWaitAsync(task, cancellationToken);
        }

        private static async Task<TResult> DoWaitAsync<TResult>(Task<TResult> task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<TResult>();

            using (cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken), false))
            {
                var inner = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);

                return await inner.ConfigureAwait(false);
            }
        }
    }
}
