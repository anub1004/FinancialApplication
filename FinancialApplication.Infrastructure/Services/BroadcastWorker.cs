using FinancialApplication.Application.DTOs.Notification;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Background worker that reads <see cref="BroadcastJob"/> items from
    /// <see cref="IBroadcastQueue"/> and fans them out as notification rows,
    /// inserting in configurable batches to avoid a single huge DB transaction.
    /// </summary>
    public class BroadcastWorker : BackgroundService
    {
        private readonly IBroadcastQueue _queue;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BroadcastWorker> _logger;

        /// <summary>Default number of rows per batch insert.</summary>
        private const int DefaultBatchSize = 100;

        public BroadcastWorker(
            IBroadcastQueue queue,
            IServiceScopeFactory scopeFactory,
            ILogger<BroadcastWorker> logger)
        {
            _queue = queue;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[BroadcastWorker] Started. Waiting for broadcast jobs...");

            while (!stoppingToken.IsCancellationRequested)
            {
                BroadcastJob job;
                try
                {
                    job = await _queue.DequeueAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break; // graceful shutdown
                }

                try
                {
                    await ProcessBroadcastAsync(job, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "[BroadcastWorker] Failed to process broadcast job {JobId}.", job.JobId);
                }
            }

            _logger.LogInformation("[BroadcastWorker] Stopped.");
        }

        private async Task ProcessBroadcastAsync(BroadcastJob job, CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            _logger.LogInformation(
                "[BroadcastWorker] Processing broadcast {JobId} — Title: \"{Title}\"",
                job.JobId, job.Request.Title);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // ── Resolve target user IDs ──────────────────────────────────────
            var query = db.Users.AsNoTracking().Where(u => u.IsActive);

            if (!string.IsNullOrWhiteSpace(job.Request.TargetRole))
            {
                query = query.Where(u => u.Role != null && u.Role.Name == job.Request.TargetRole);
            }

            if (!string.IsNullOrWhiteSpace(job.Request.TargetPlanSlug))
            {
                var now = DateTime.UtcNow;
                var planUserIds = await db.UserSubscriptions
                    .AsNoTracking()
                    .Include(s => s.Plan)
                    .Where(s =>
                        (s.Status == SubscriptionStatusEnum.Active ||
                         s.Status == SubscriptionStatusEnum.Trial) &&
                        s.EndDate > now &&
                        s.Plan.Slug == job.Request.TargetPlanSlug)
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToListAsync(ct);

                query = query.Where(u => planUserIds.Contains(u.Id));
            }

            var userIds = await query.Select(u => u.Id).ToListAsync(ct);

            if (userIds.Count == 0)
            {
                _logger.LogWarning(
                    "[BroadcastWorker] Broadcast {JobId} matched 0 users. Skipping.", job.JobId);
                return;
            }

            // ── Fan-out in batches ───────────────────────────────────────────
            var batchSize = job.Request.BatchSize > 0
                ? Math.Min(job.Request.BatchSize, 500)
                : DefaultBatchSize;

            var createdAt = DateTime.UtcNow;
            var totalInserted = 0;

            foreach (var chunk in userIds.Chunk(batchSize))
            {
                var notifications = chunk.Select(uid => new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = uid,
                    Title = job.Request.Title,
                    Message = job.Request.Message,
                    Type = NotificationTypeEnum.AdminBroadcast,
                    IsRead = false,
                    IsGlobal = true,
                    CreatedAt = createdAt
                }).ToList();

                await db.Notifications.AddRangeAsync(notifications, ct);
                await db.SaveChangesAsync(ct);

                totalInserted += notifications.Count;

                _logger.LogDebug(
                    "[BroadcastWorker] Job {JobId} — batch inserted {BatchCount} rows ({TotalInserted}/{TotalUsers})",
                    job.JobId, notifications.Count, totalInserted, userIds.Count);
            }

            sw.Stop();
            _logger.LogInformation(
                "[BroadcastWorker] Broadcast {JobId} complete — {Total} notifications sent to {Recipients} users in {Elapsed}ms.",
                job.JobId, totalInserted, userIds.Count, sw.ElapsedMilliseconds);
        }
    }
}
