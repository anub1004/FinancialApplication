using System;
using System.Collections.Generic;

namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Response DTO containing the resolved list of allowed features for a user.
    /// Used by GET /api/subscription/my-features.
    /// </summary>
    public class UserFeaturesResponse
    {
        public Guid UserId { get; set; }
        public string PlanName { get; set; } = string.Empty;
        public string PlanSlug { get; set; } = string.Empty;
        public List<string> FeatureKeys { get; set; } = new List<string>();
    }
}
