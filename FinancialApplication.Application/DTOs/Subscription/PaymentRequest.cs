using System;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Request DTO for processing a payment through the payment gateway.
    /// Contains all information needed for a real payment gateway (Razorpay/Stripe).
    /// </summary>
    public class PaymentRequest
    {
        /// <summary>
        /// The user making the payment.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The subscription this payment is for.
        /// </summary>
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Amount to charge.
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (e.g., "INR", "USD").
        /// </summary>
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Payment method identifier (e.g., "Card", "UPI", "NetBanking").
        /// Null for simulated/free payments.
        /// </summary>
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// Description for the payment (shown on gateway/invoice).
        /// </summary>
        public string? Description { get; set; }
    }
}
