using FinancialApplication.Application.DTOs.Notification;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Background hosted service that runs once every 24 hours and scans
    /// for subscription lifecycle events that require automated notifications:
    ///
    ///   • Trial ending in ≤ 3 days
    ///   • Subscription renewal due in ≤ 3 days
    ///   • Subscription expired today (new expirations since last run)
    ///   • Payment failures recorded since last run
    ///   • Payment successes recorded since last run
    ///
    /// Uses <see cref="IServiceScopeFactory"/> to safely resolve the scoped
    /// <see cref="AppDbContext"/> inside a singleton hosted service.
    /// </summary>
    public class SubscriptionNotificationJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<SubscriptionNotificationJob> _logger;

        // How often the job runs.
        private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

        // How many days ahead to warn about expiry / renewal.
        private const int WarnDaysAhead = 3;

        public SubscriptionNotificationJob(
            IServiceScopeFactory scopeFactory,
            ILogger<SubscriptionNotificationJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation(
                "[NotificationJob] Started. Will run every {Interval}h.", Interval.TotalHours);

            // Give the application a moment to finish starting up before first run.
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

            using var timer = new PeriodicTimer(Interval);

            do
            {
                try
                {
                    await RunAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[NotificationJob] Unhandled error during notification sweep.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        // ────────────────────────────────────────────────────────────────────
        private async Task RunAsync(CancellationToken ct)
        {
            _logger.LogInformation("[NotificationJob] Running subscription notification sweep at {Time}.", DateTime.UtcNow);

            await using var scope = _scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

            var now = DateTime.UtcNow;
            var today = now.Date;
            var warnDate = today.AddDays(WarnDaysAhead);

            // ── 1. Trial Ending Soon ─────────────────────────────────────
            var trialEndingSubs = await db.UserSubscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Where(s =>
                    s.Status == SubscriptionStatusEnum.Trial &&
                    s.TrialEndDate.HasValue &&
                    s.TrialEndDate.Value.Date >= today &&
                    s.TrialEndDate.Value.Date <= warnDate)
                .ToListAsync(ct);

            foreach (var sub in trialEndingSubs)
            {
                // Avoid duplicate: check if we already sent this notification today
                var alreadySent = await db.Notifications.AnyAsync(n =>
                    n.UserId == sub.UserId &&
                    n.Type == NotificationTypeEnum.TrialEnding &&
                    n.RelatedEntityId == sub.Id &&
                    n.CreatedAt.Date == today, ct);

                if (!alreadySent)
                {
                    var daysLeft = (sub.TrialEndDate!.Value.Date - today).Days;
                    await notificationService.SendSubscriptionNotificationAsync(
                        userId: sub.UserId,
                        type: NotificationTypeEnum.TrialEnding,
                        title: NotificationMessages.TrialEndingTitle(sub.Plan.Name, daysLeft),
                        message: NotificationMessages.TrialEndingMessage(
                            sub.Plan.Name, sub.TrialEndDate.Value.ToString("dd MMM yyyy")),
                        relatedEntityId: sub.Id);

                    _logger.LogDebug("[NotificationJob] TrialEnding sent → UserId={UserId}", sub.UserId);
                }
            }

            // ── 2. Subscription Renewal Due Soon ─────────────────────────
            var renewalSubs = await db.UserSubscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Where(s =>
                    s.Status == SubscriptionStatusEnum.Active &&
                    s.AutoRenew &&
                    s.NextRenewalDate.HasValue &&
                    s.NextRenewalDate.Value.Date >= today &&
                    s.NextRenewalDate.Value.Date <= warnDate)
                .ToListAsync(ct);

            foreach (var sub in renewalSubs)
            {
                var alreadySent = await db.Notifications.AnyAsync(n =>
                    n.UserId == sub.UserId &&
                    n.Type == NotificationTypeEnum.SubscriptionRenewal &&
                    n.RelatedEntityId == sub.Id &&
                    n.CreatedAt.Date == today, ct);

                if (!alreadySent)
                {
                    var daysLeft = (sub.NextRenewalDate!.Value.Date - today).Days;
                    await notificationService.SendSubscriptionNotificationAsync(
                        userId: sub.UserId,
                        type: NotificationTypeEnum.SubscriptionRenewal,
                        title: NotificationMessages.SubscriptionRenewalTitle(sub.Plan.Name, daysLeft),
                        message: NotificationMessages.SubscriptionRenewalMessage(
                            sub.Plan.Name, sub.NextRenewalDate.Value.ToString("dd MMM yyyy")),
                        relatedEntityId: sub.Id);

                    _logger.LogDebug("[NotificationJob] SubscriptionRenewal sent → UserId={UserId}", sub.UserId);
                }
            }

            // ── 3. Subscription Expired Today ────────────────────────────
            var expiredSubs = await db.UserSubscriptions
                .AsNoTracking()
                .Include(s => s.Plan)
                .Where(s =>
                    s.Status == SubscriptionStatusEnum.Active &&
                    s.EndDate.Date == today)
                .ToListAsync(ct);

            foreach (var sub in expiredSubs)
            {
                var alreadySent = await db.Notifications.AnyAsync(n =>
                    n.UserId == sub.UserId &&
                    n.Type == NotificationTypeEnum.SubscriptionExpired &&
                    n.RelatedEntityId == sub.Id &&
                    n.CreatedAt.Date == today, ct);

                if (!alreadySent)
                {
                    await notificationService.SendSubscriptionNotificationAsync(
                        userId: sub.UserId,
                        type: NotificationTypeEnum.SubscriptionExpired,
                        title: NotificationMessages.SubscriptionExpiredTitle(sub.Plan.Name),
                        message: NotificationMessages.SubscriptionExpiredMessage(sub.Plan.Name),
                        relatedEntityId: sub.Id);

                    _logger.LogDebug("[NotificationJob] SubscriptionExpired sent → UserId={UserId}", sub.UserId);
                }
            }

            // ── 4. Payment Failures (last 24 h) ─────────────────────────
            var since = now.AddHours(-25); // slightly wider window to avoid gaps

            var failedPayments = await db.Payments
                .AsNoTracking()
                .Include(p => p.UserSubscription)
                    .ThenInclude(s => s!.Plan)
                .Where(p =>
                    p.Status == PaymentStatusEnum.Failed &&
                    p.CreatedAt >= since)
                .ToListAsync(ct);

            foreach (var payment in failedPayments)
            {
                var alreadySent = await db.Notifications.AnyAsync(n =>
                    n.RelatedEntityId == payment.Id &&
                    n.Type == NotificationTypeEnum.PaymentFailed, ct);

                if (!alreadySent && payment.UserSubscription != null)
                {
                    await notificationService.SendSubscriptionNotificationAsync(
                        userId: payment.UserSubscription.UserId,
                        type: NotificationTypeEnum.PaymentFailed,
                        title: NotificationMessages.PaymentFailedTitle,
                        message: NotificationMessages.PaymentFailedMessage(
                            payment.Amount, payment.UserSubscription.Plan.Name),
                        relatedEntityId: payment.Id);

                    _logger.LogDebug("[NotificationJob] PaymentFailed sent → PaymentId={PaymentId}", payment.Id);
                }
            }

            // ── 5. Payment Successes (last 24 h) ─────────────────────────
            var successPayments = await db.Payments
                .AsNoTracking()
                .Include(p => p.UserSubscription)
                    .ThenInclude(s => s!.Plan)
                .Where(p =>
                    p.Status == PaymentStatusEnum.Completed &&
                    p.CreatedAt >= since)
                .ToListAsync(ct);

            foreach (var payment in successPayments)
            {
                var alreadySent = await db.Notifications.AnyAsync(n =>
                    n.RelatedEntityId == payment.Id &&
                    n.Type == NotificationTypeEnum.PaymentSuccess, ct);

                if (!alreadySent && payment.UserSubscription != null)
                {
                    await notificationService.SendSubscriptionNotificationAsync(
                        userId: payment.UserSubscription.UserId,
                        type: NotificationTypeEnum.PaymentSuccess,
                        title: NotificationMessages.PaymentSuccessTitle,
                        message: NotificationMessages.PaymentSuccessMessage(
                            payment.Amount, payment.UserSubscription.Plan.Name),
                        relatedEntityId: payment.Id);

                    _logger.LogDebug("[NotificationJob] PaymentSuccess sent → PaymentId={PaymentId}", payment.Id);
                }
            }

            _logger.LogInformation(
                "[NotificationJob] Sweep complete. " +
                "TrialEnding={TrialCount}, Renewals={RenewalCount}, Expired={ExpiredCount}, " +
                "PayFailed={FailCount}, PaySuccess={SuccessCount}",
                trialEndingSubs.Count, renewalSubs.Count, expiredSubs.Count,
                failedPayments.Count, successPayments.Count);
        }
    }
}
