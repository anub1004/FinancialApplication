using System;
using System.Threading.Tasks;
using FinancialApplication.Application.DTOs.Subscription;
using FinancialApplication.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace FinancialApplication.Infrastructure.Services
{
    /// <summary>
    /// Simulated payment gateway that auto-approves all payments.
    /// 
    /// This is the default implementation used until a real payment gateway
    /// (Razorpay, Stripe, etc.) is integrated.
    /// 
    /// To integrate a real gateway:
    ///   1. Create a new class implementing IPaymentGateway (e.g., RazorpayPaymentGateway)
    ///   2. Change the DI registration in Program.cs:
    ///      builder.Services.AddScoped<IPaymentGateway, RazorpayPaymentGateway>();
    ///   3. No other code changes needed — SubscriptionService uses IPaymentGateway
    /// </summary>
    public class SimulatedPaymentGateway : IPaymentGateway
    {
        private readonly ILogger<SimulatedPaymentGateway> _logger;

        public SimulatedPaymentGateway(ILogger<SimulatedPaymentGateway> logger)
        {
            _logger = logger;
        }

        public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
        {
            // Generate a simulated transaction reference
            var transactionRef = $"SIM_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";

            _logger.LogInformation(
                "Simulated payment processed: UserId={UserId}, Amount={Amount} {Currency}, TxRef={TransactionRef}",
                request.UserId, request.Amount, request.Currency, transactionRef);

            var result = PaymentResult.Succeeded(
                transactionRef: transactionRef,
                gatewayResponse: $"{{\"simulated\":true,\"amount\":{request.Amount},\"currency\":\"{request.Currency}\"}}"
            );

            return Task.FromResult(result);
        }
    }
}
