using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FinancialApplication.Domain.Domain.Enums
{
    /// <summary>
    /// Represents the status of an invoice.
    /// Used in Invoice entity to track invoice lifecycle.
    /// </summary>
    public enum InvoiceStatusEnum
    {
        Draft = 1,
        Issued = 2,
        Paid = 3,
        Void = 4
    }
}
