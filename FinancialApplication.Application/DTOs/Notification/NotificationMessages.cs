namespace FinancialApplication.Application.DTOs.Notification
{
    /// <summary>
    /// Centralized notification message templates.
    /// Eliminates hardcoded strings scattered across services and background jobs.
    /// </summary>
    public static class NotificationMessages
    {
        // ── Trial Ending ─────────────────────────────────────────────────────

        public static string TrialEndingTitle(string planName, int daysLeft) =>
            $"Your free trial ends in {daysLeft} day{(daysLeft == 1 ? "" : "s")}";

        public static string TrialEndingMessage(string planName, string formattedEndDate) =>
            $"Your {planName} trial expires on {formattedEndDate}. " +
            "Upgrade now to keep full access to all your features.";

        // ── Subscription Renewal ─────────────────────────────────────────────

        public static string SubscriptionRenewalTitle(string planName, int daysLeft) =>
            $"Your {planName} subscription renews in {daysLeft} day{(daysLeft == 1 ? "" : "s")}";

        public static string SubscriptionRenewalMessage(string planName, string formattedRenewalDate) =>
            $"Your {planName} plan will automatically renew on {formattedRenewalDate}. " +
            "You can manage your subscription in the Billing section.";

        // ── Subscription Expired ─────────────────────────────────────────────

        public static string SubscriptionExpiredTitle(string planName) =>
            $"Your {planName} subscription has expired";

        public static string SubscriptionExpiredMessage(string planName) =>
            $"Your {planName} plan expired today. " +
            "Renew now to restore full access to all premium features.";

        // ── Payment Failed ───────────────────────────────────────────────────

        public const string PaymentFailedTitle = "Payment failed";

        public static string PaymentFailedMessage(decimal amount, string planName) =>
            $"We couldn't process your payment of ₹{amount:0.00} for your {planName} plan. " +
            "Please update your payment method to avoid service interruption.";

        // ── Payment Success ──────────────────────────────────────────────────

        public const string PaymentSuccessTitle = "Payment received";

        public static string PaymentSuccessMessage(decimal amount, string planName) =>
            $"Payment of ₹{amount:0.00} for your {planName} plan was successful. " +
            "Your subscription is active.";
    }
}
