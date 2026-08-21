using System;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Data Transfer Object representing a summary of a plan.
    /// Used for lightweight dropdowns or user plans options lists.
    /// </summary>
    public class PlanSummaryDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public string Currency { get; set; } = "INR";
    }
}
