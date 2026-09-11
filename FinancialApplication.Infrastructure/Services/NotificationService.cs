using FinancialApplication.Application.DTOs.Notification;
using FinancialApplication.Application.Interfaces;
using FinancialApplication.Domain.Domain.Entity;
using FinancialApplication.Domain.Domain.Enums;
using FinancialApplication.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Implements <see cref="INotificationService"/>.
    /// Handles admin broadcasts (fan-out), automated subscription events,
    /// and user-facing read-side queries.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly IBroadcastQueue _broadcastQueue;

        public NotificationService(AppDbContext context, IBroadcastQueue broadcastQueue)
        {
            _context = context;
            _broadcastQueue = broadcastQueue;
        }

        // ════════════════════════════════════════════════════════════════════
        // ADMIN BROADCAST
        // ════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<BroadcastResultDto> SendAdminBroadcastAsync(
            SendBroadcastRequest request,
            Guid adminId)
        {
            // ── Resolve target user IDs ───────────────────────────────────
            var query = _context.Users.AsNoTracking()
                .Where(u => u.IsActive);

            // Optional: filter by role name
            if (!string.IsNullOrWhiteSpace(request.TargetRole))
            {
                query = query.Where(u => u.Role != null && u.Role.Name == request.TargetRole);
            }

            // Optional: filter by current active plan slug
            if (!string.IsNullOrWhiteSpace(request.TargetPlanSlug))
            {
                var now = DateTime.UtcNow;
                // Users who currently have an active/trial subscription on the named plan
                var planUserIds = await _context.UserSubscriptions
                    .AsNoTracking()
                    .Include(s => s.Plan)
                    .Where(s =>
                        (s.Status == SubscriptionStatusEnum.Active ||
                         s.Status == SubscriptionStatusEnum.Trial) &&
                        s.EndDate > now &&
                        s.Plan.Slug == request.TargetPlanSlug)
                    .Select(s => s.UserId)
                    .Distinct()
                    .ToListAsync();

                query = query.Where(u => planUserIds.Contains(u.Id));
            }

            var userIds = await query.Select(u => u.Id).ToListAsync();

            if (userIds.Count == 0)
                return new BroadcastResultDto
                {
                    RecipientsCount = 0,
                    Title = request.Title,
                    DispatchedAt = DateTime.UtcNow
                };

            // ── Fan-out: insert in batches to avoid one huge DB transaction ─
            var batchSize = request.BatchSize > 0
                ? Math.Min(request.BatchSize, 500)
                : 100;

            var now2 = DateTime.UtcNow;
            var totalInserted = 0;

            foreach (var chunk in userIds.Chunk(batchSize))
            {
                var batch = chunk.Select(uid => new Notification
                {
                    Id = Guid.NewGuid(),
                    UserId = uid,
                    Title = request.Title,
                    Message = request.Message,
                    Type = NotificationTypeEnum.AdminBroadcast,
                    IsRead = false,
                    IsGlobal = true,
                    CreatedAt = now2
                }).ToList();

                await _context.Notifications.AddRangeAsync(batch);
                await _context.SaveChangesAsync();
                totalInserted += batch.Count;
            }

            return new BroadcastResultDto
            {
                RecipientsCount = totalInserted,
                Title = request.Title,
                DispatchedAt = now2
            };
        }

        /// <inheritdoc/>
        public async Task<BroadcastQueuedDto> EnqueueBroadcastAsync(
            SendBroadcastRequest request,
            Guid adminId)
        {
            var job = new BroadcastJob
            {
                Request = request,
                AdminId = adminId
            };

            await _broadcastQueue.EnqueueAsync(job);

            return new BroadcastQueuedDto
            {
                JobId = job.JobId,
                Title = request.Title,
                EnqueuedAt = job.EnqueuedAt
            };
        }

        // ════════════════════════════════════════════════════════════════════
        // AUTOMATED / SYSTEM NOTIFICATIONS
        // ════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task SendSubscriptionNotificationAsync(
            Guid userId,
            NotificationTypeEnum type,
            string title,
            string message,
            Guid? relatedEntityId = null)
        {
            var notification = new Notification
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                IsRead = false,
                IsGlobal = false,
                RelatedEntityId = relatedEntityId,
                CreatedAt = DateTime.UtcNow
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        // ════════════════════════════════════════════════════════════════════
        // USER READ-SIDE
        // ════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<NotificationPagedResponse> GetUserNotificationsAsync(
            Guid userId,
            int page,
            int pageSize,
            bool? onlyUnread = null)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            var baseQuery = _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId);

            if (onlyUnread == true)
                baseQuery = baseQuery.Where(n => !n.IsRead);

            var totalCount = await baseQuery.CountAsync();
            var unreadCount = await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);

            var items = await baseQuery
                .OrderByDescending(n => n.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type,
                    TypeLabel = GetTypeLabel(n.Type),
                    IsRead = n.IsRead,
                    IsGlobal = n.IsGlobal,
                    RelatedEntityId = n.RelatedEntityId,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt
                })
                .ToListAsync();

            return new NotificationPagedResponse
            {
                Items = items,
                TotalCount = totalCount,
                UnreadCount = unreadCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        /// <inheritdoc/>
        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }

        /// <inheritdoc/>
        public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return false;
            if (notification.IsRead) return true;   // already read — idempotent

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        /// <inheritdoc/>
        public async Task MarkAllAsReadAsync(Guid userId)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unread.Count == 0) return;

            var now = DateTime.UtcNow;
            foreach (var n in unread)
            {
                n.IsRead = true;
                n.ReadAt = now;
            }

            await _context.SaveChangesAsync();
        }

        // ════════════════════════════════════════════════════════════════════
        // BROADCAST HISTORY (admin)
        // ════════════════════════════════════════════════════════════════════

        /// <inheritdoc/>
        public async Task<List<BroadcastHistoryDto>> GetBroadcastHistoryAsync(int page, int pageSize)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 50);

            // Group admin broadcasts by Title + Message + CreatedAt (minute precision)
            // and count the recipients. Newest batches first.
            var result = await _context.Notifications
                .AsNoTracking()
                .Where(n => n.IsGlobal)
                .GroupBy(n => new
                {
                    n.Title,
                    n.Message,
                    // Truncate to minute precision for grouping broadcast batches
                    Bucket = new DateTime(
                        n.CreatedAt.Year, n.CreatedAt.Month, n.CreatedAt.Day,
                        n.CreatedAt.Hour, n.CreatedAt.Minute, 0)
                })
                .OrderByDescending(g => g.Key.Bucket)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(g => new BroadcastHistoryDto
                {
                    Title = g.Key.Title,
                    Message = g.Key.Message,
                    RecipientsCount = g.Count(),
                    CreatedAt = g.Key.Bucket
                })
                .ToListAsync();

            return result;
        }

        // ── Helpers ─────────────────────────────────────────────────────────

        private static string GetTypeLabel(NotificationTypeEnum type) => type switch
        {
            NotificationTypeEnum.AdminBroadcast      => "Announcement",
            NotificationTypeEnum.SubscriptionRenewal => "Renewal Reminder",
            NotificationTypeEnum.SubscriptionExpired => "Subscription Expired",
            NotificationTypeEnum.TrialEnding         => "Trial Ending",
            NotificationTypeEnum.PlanChanged         => "Plan Changed",
            NotificationTypeEnum.PaymentFailed       => "Payment Failed",
            NotificationTypeEnum.PaymentSuccess      => "Payment Success",
            NotificationTypeEnum.SystemAlert         => "System Alert",
            _                                        => "Notification"
        };
    }
}
