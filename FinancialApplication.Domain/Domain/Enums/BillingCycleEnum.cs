using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Domain.Domain.Enums
{
    /// <summary>
    /// Represents the billing cycle for a subscription.
    /// Determines how often the user is charged.
    /// </summary>
    public enum BillingCycleEnum
    {
        Monthly = 1,
        Annual = 2,
        Lifetime = 3
    }
}
