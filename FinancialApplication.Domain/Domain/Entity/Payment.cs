using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents a payment transaction for a subscription.
    /// Supports future payment gateway integration (Razorpay/Stripe).
    /// Primary key: Id (GUID)
    /// Foreign keys: UserId, SubscriptionId
    /// </summary>
    public class Payment
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The user who made this payment.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// The subscription this payment is for.
        /// </summary>
        [Required]
        public Guid SubscriptionId { get; set; }

        /// <summary>
        /// Amount charged for this payment.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Currency code (e.g., "INR", "USD").
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Current status of the payment.
        /// Stored as string in DB (configured in Phase 2).
        /// </summary>
        [Required]
        public PaymentStatusEnum Status { get; set; }

        /// <summary>
        /// Payment method used (e.g., "Card", "UPI", "NetBanking").
        /// Null for free plans or system-generated payments.
        /// </summary>
        [MaxLength(50)]
        public string? PaymentMethod { get; set; }

        /// <summary>
        /// External payment gateway transaction reference ID.
        /// Used for reconciliation with Razorpay/Stripe.
        /// </summary>
        [MaxLength(200)]
        public string? TransactionRef { get; set; }

        /// <summary>
        /// Raw JSON response from the payment gateway.
        /// Stored for debugging and dispute resolution.
        /// </summary>
        public string? GatewayResponse { get; set; }

        /// <summary>
        /// When the payment was successfully completed.
        /// Null if payment is still pending or failed.
        /// </summary>
        public DateTime? PaidAt { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("SubscriptionId")]
        public virtual UserSubscription UserSubscription { get; set; } = null!;

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
