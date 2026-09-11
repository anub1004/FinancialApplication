using System;
using System.ComponentModel.DataAnnotations;

namespace FinancialApplication.Application.DTOs.Budget
{
    /// <summary>
    /// Budget data returned to the client.
    /// Includes actual spending amount computed from transactions.
    /// </summary>
    public class BudgetDto
    {
        public Guid Id { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal MonthlyLimit { get; set; }
        public int AlertThresholdPercent { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Currency { get; set; } = "INR";

        /// <summary>
        /// Actual amount spent in this category for the given month/year.
        /// Computed from the Transactions table.
        /// </summary>
        public decimal AmountSpent { get; set; }

        /// <summary>
        /// Remaining budget = MonthlyLimit - AmountSpent.
        /// </summary>
        public decimal Remaining => MonthlyLimit - AmountSpent;

        /// <summary>
        /// Percentage of budget consumed (0-100+).
        /// </summary>
        public decimal PercentUsed => MonthlyLimit > 0
            ? Math.Round(AmountSpent / MonthlyLimit * 100, 1)
            : 0;

        /// <summary>
        /// Whether spending has exceeded the alert threshold.
        /// </summary>
        public bool IsAlertTriggered => PercentUsed >= AlertThresholdPercent;

        /// <summary>
        /// Whether spending has exceeded the budget limit.
        /// </summary>
        public bool IsOverBudget => AmountSpent > MonthlyLimit;
    }

    /// <summary>
    /// DTO for creating a new budget.
    /// </summary>
    public class CreateBudgetDto
    {
        [Required]
        [MaxLength(100)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Monthly limit must be greater than 0.")]
        public decimal MonthlyLimit { get; set; }

        [Range(1, 100)]
        public int AlertThresholdPercent { get; set; } = 80;

        [Required]
        [Range(1, 12)]
        public int Month { get; set; }

        [Required]
        public int Year { get; set; }

        [MaxLength(10)]
        public string Currency { get; set; } = "INR";
    }

    /// <summary>
    /// DTO for updating an existing budget.
    /// </summary>
    public class UpdateBudgetDto
    {
        [Range(0.01, double.MaxValue, ErrorMessage = "Monthly limit must be greater than 0.")]
        public decimal? MonthlyLimit { get; set; }

        [Range(1, 100)]
        public int? AlertThresholdPercent { get; set; }

        [MaxLength(10)]
        public string? Currency { get; set; }
    }

    /// <summary>
    /// Alert information for a single budget category.
    /// </summary>
    public class BudgetAlertDto
    {
        public Guid BudgetId { get; set; }
        public string Category { get; set; } = string.Empty;
        public decimal MonthlyLimit { get; set; }
        public decimal AmountSpent { get; set; }
        public decimal PercentUsed { get; set; }
        public string AlertLevel { get; set; } = string.Empty; // "Warning" or "Exceeded"
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Overall budget summary for a given month.
    /// </summary>
    public class BudgetSummaryDto
    {
        public int Month { get; set; }
        public int Year { get; set; }
        public int TotalBudgets { get; set; }
        public decimal TotalBudgetLimit { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalRemaining => TotalBudgetLimit - TotalSpent;
        public int AlertCount { get; set; }
        public int OverBudgetCount { get; set; }
    }
}
