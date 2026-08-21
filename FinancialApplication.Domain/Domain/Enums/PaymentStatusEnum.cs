using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Domain.Domain.Enums
{
    /// <summary>
    /// Represents the status of a payment transaction.
    /// Used in Payment entity to track payment processing state.
    /// </summary>
    public enum PaymentStatusEnum
    {
        Pending = 1,
        Completed = 2,
        Failed = 3,
        Refunded = 4
    }
}
