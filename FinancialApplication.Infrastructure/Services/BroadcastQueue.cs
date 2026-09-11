using FinancialApplication.Application.DTOs.Notification;
using FinancialApplication.Application.Interfaces;
using System.Threading.Channels;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// In-process broadcast queue backed by <see cref="Channel{T}"/>.
    /// Bounded to 100 pending jobs for back-pressure; blocks writers if full.
    /// Registered as a Singleton in DI.
    /// </summary>
    public class BroadcastQueue : IBroadcastQueue
    {
        private readonly Channel<BroadcastJob> _channel;

        public BroadcastQueue()
        {
            var options = new BoundedChannelOptions(capacity: 100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,   // only BroadcastWorker reads
                SingleWriter = false   // any controller thread can write
            };
            _channel = Channel.CreateBounded<BroadcastJob>(options);
        }

        /// <inheritdoc/>
        public async ValueTask EnqueueAsync(BroadcastJob job, CancellationToken ct = default)
        {
            await _channel.Writer.WriteAsync(job, ct);
        }

        /// <inheritdoc/>
        public async ValueTask<BroadcastJob> DequeueAsync(CancellationToken ct)
        {
            return await _channel.Reader.ReadAsync(ct);
        }
    }
}
