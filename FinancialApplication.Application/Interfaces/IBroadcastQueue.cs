using FinancialApplication.Application.DTOs.Notification;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// In-process queue for admin broadcast jobs.
    /// Backed by <see cref="System.Threading.Channels.Channel{T}"/> —
    /// zero external dependencies, bounded capacity for back-pressure.
    /// </summary>
    public interface IBroadcastQueue
    {
        /// <summary>
        /// Enqueue a broadcast job for background processing.
        /// Returns immediately. Non-blocking unless the channel is full.
        /// </summary>
        ValueTask EnqueueAsync(BroadcastJob job, CancellationToken ct = default);

        /// <summary>
        /// Dequeue the next broadcast job. Blocks asynchronously until a job is available.
        /// Used by the <c>BroadcastWorker</c> hosted service.
        /// </summary>
        ValueTask<BroadcastJob> DequeueAsync(CancellationToken ct);
    }
}
