using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Application.DTOs.Notification
{
    /// <summary>
    /// Represents a single notification returned to the client.
    /// </summary>
    public class NotificationDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationTypeEnum Type { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsGlobal { get; set; }
        public Guid? RelatedEntityId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
    }

    /// <summary>
    /// Paginated response for user notification list.
    /// </summary>
    public class NotificationPagedResponse
    {
        public List<NotificationDto> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int UnreadCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
