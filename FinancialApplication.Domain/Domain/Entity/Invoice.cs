using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Domain.Domain.Entity
{
    /// <summary>
    /// Represents an invoice generated for a payment.
    /// Auto-generated when a payment is completed.
    /// Primary key: Id (GUID)
    /// Foreign keys: UserId, PaymentId (optional)
    /// </summary>
    public class Invoice
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The user this invoice belongs to.
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// The payment associated with this invoice.
        /// Null for draft invoices or if payment is deleted.
        /// </summary>
        public Guid? PaymentId { get; set; }

        /// <summary>
        /// Unique invoice number in format "INV-{year}-{sequence}".
        /// e.g., "INV-2026-0001"
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string InvoiceNumber { get; set; } = string.Empty;

        /// <summary>
        /// Base amount before tax.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
        /// Tax amount applied to this invoice.
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Tax { get; set; } = 0;

        /// <summary>
        /// Total amount including tax (Amount + Tax).
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Currency code (e.g., "INR", "USD").
        /// </summary>
        [Required]
        [MaxLength(10)]
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Current status of the invoice.
        /// Stored as string in DB (configured in Phase 2).
        /// </summary>
        [Required]
        public InvoiceStatusEnum Status { get; set; }

        /// <summary>
        /// When the invoice was issued to the user.
        /// </summary>
        [Required]
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When payment is due for this invoice.
        /// </summary>
        [Required]
        public DateTime DueDate { get; set; }

        /// <summary>
        /// When the invoice was paid.
        /// Null if not yet paid.
        /// </summary>
        public DateTime? PaidAt { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("PaymentId")]
        public virtual Payment? Payment { get; set; }
    }
}
