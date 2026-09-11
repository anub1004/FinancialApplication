namespace FinancialApplication.Domain.Domain.Enums
{
    /// <summary>
    /// Classifies the source / purpose of a notification record.
    /// </summary>
    public enum NotificationTypeEnum
    {
        /// <summary>Admin-initiated broadcast message sent to all (or filtered) users.</summary>
        AdminBroadcast,

        /// <summary>Reminder that the subscription auto-renews soon.</summary>
        SubscriptionRenewal,

        /// <summary>The subscription has expired and access has been reduced.</summary>
        SubscriptionExpired,

        /// <summary>The free trial is ending within 3 days.</summary>
        TrialEnding,

        /// <summary>The user's active plan was upgraded, downgraded or changed.</summary>
        PlanChanged,

        /// <summary>A payment attempt failed (card declined, insufficient funds, etc.).</summary>
        PaymentFailed,

        /// <summary>A payment was received and the subscription was activated/renewed.</summary>
        PaymentSuccess,

        /// <summary>General platform / system announcement.</summary>
        SystemAlert
    }
}
