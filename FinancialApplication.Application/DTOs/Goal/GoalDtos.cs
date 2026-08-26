using System;
using System.ComponentModel.DataAnnotations;
using FinancialApplication.Domain.Domain.Enums;

namespace FinancialApplication.Application.DTOs.Goal
{
    // ── Create ───────────────────────────────────────────────────────────────
    public class CreateGoalDto
    {
        [Required]
        [MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Target amount must be greater than 0.")]
        public decimal TargetAmount { get; set; }

        public decimal CurrentAmount { get; set; } = 0;

        [Required]
        public DateTime Deadline { get; set; }

        [MaxLength(10)]
        public string? Icon { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "INR";
    }

    // ── Update ───────────────────────────────────────────────────────────────
    public class UpdateGoalDto
    {
        [MaxLength(255)]
        public string? Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(0.01, double.MaxValue)]
        public decimal? TargetAmount { get; set; }

        public DateTime? Deadline { get; set; }

        [MaxLength(10)]
        public string? Icon { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }
    }

    // ── Contribute ───────────────────────────────────────────────────────────
    public class GoalContributionDto
    {
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Contribution amount must be greater than 0.")]
        public decimal Amount { get; set; }
    }

    // ── Update Status ────────────────────────────────────────────────────────
    public class GoalStatusUpdateDto
    {
        [Required]
        public GoalStatusEnum Status { get; set; }
    }

    // ── Response ─────────────────────────────────────────────────────────────
    public class GoalDto
    {
        public Guid GoalId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal ProgressPercentage { get; set; }
        public DateTime Deadline { get; set; }
        public GoalStatusEnum Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public string Currency { get; set; } = "INR";
        public int DaysRemaining { get; set; }
        public decimal? MonthlyTargetToComplete { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
