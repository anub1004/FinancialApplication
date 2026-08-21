using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Domain.Domain.Enums
{
    /// <summary>
    /// Represents the status of a user's subscription.
    /// Used in UserSubscription entity to track lifecycle state.
    /// </summary>
    public enum SubscriptionStatusEnum
    {
        Active = 1,
        Trial = 2,
        Expired = 3,
        Cancelled = 4,
        PastDue = 5,
        Suspended = 6
    }
}
