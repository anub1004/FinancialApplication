using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;

namespace FinancialApplication.Application.Interfaces
{
    /// <summary>
    /// Abstraction for payment processing.
    /// 
    /// Implementations:
    ///   - SimulatedPaymentGateway  → auto-approves (current, no real gateway)
    ///   - RazorpayPaymentGateway   → future Razorpay integration
    ///   - StripePaymentGateway     → future Stripe integration
    /// 
    /// The subscription service calls this during upgrade/subscribe flows.
    /// Swap the DI registration to switch gateways — zero code changes needed elsewhere.
    /// </summary>
    public interface IPaymentGateway
    {
        /// <summary>
        /// Process a payment for a subscription action (subscribe, upgrade, renew).
        /// Returns a PaymentResult indicating success/failure and a transaction reference.
        /// </summary>
        Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    }
}
