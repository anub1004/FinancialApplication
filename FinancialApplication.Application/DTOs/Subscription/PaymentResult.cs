namespace FinancialApplication.Application.DTOs.Subscription
{
    /// <summary>
    /// Result DTO returned by the payment gateway after processing a payment.
    /// </summary>
    public class PaymentResult
    {
        /// <summary>
        /// Whether the payment was successfully processed.
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// External transaction reference from the payment gateway (e.g., Razorpay payment ID).
        /// Null for simulated payments.
        /// </summary>
        public string? TransactionRef { get; set; }

        /// <summary>
        /// Raw gateway response (JSON string) for debugging and audit.
        /// Null for simulated payments.
        /// </summary>
        public string? GatewayResponse { get; set; }

        /// <summary>
        /// Error message if payment failed.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Creates a successful payment result.
        /// </summary>
        public static PaymentResult Succeeded(string? transactionRef = null, string? gatewayResponse = null)
            => new()
            {
                Success = true,
                TransactionRef = transactionRef,
                GatewayResponse = gatewayResponse
            };

        /// <summary>
        /// Creates a failed payment result.
        /// </summary>
        public static PaymentResult Failed(string errorMessage, string? gatewayResponse = null)
            => new()
            {
                Success = false,
                ErrorMessage = errorMessage,
                GatewayResponse = gatewayResponse
            };
    }
}
