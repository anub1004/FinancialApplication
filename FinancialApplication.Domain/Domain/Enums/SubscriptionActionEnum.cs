using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Domain.Domain.Enums
{
    /// <summary>
    /// Represents an action performed on a subscription.
    /// Used in SubscriptionHistory to track all lifecycle events.
    /// </summary>
    public enum SubscriptionActionEnum
    {
        Created = 1,
        Upgraded = 2,
        Downgraded = 3,
        Renewed = 4,
        Cancelled = 5,
        Expired = 6,
        Reactivated = 7
    }
}
