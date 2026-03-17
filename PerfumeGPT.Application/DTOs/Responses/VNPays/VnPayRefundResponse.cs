using System;

namespace PerfumeGPT.Application.DTOs.Responses.VNPays
{
    public class VnPayRefundResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; } = string.Empty;

        public Guid PaymentId { get; set; }
        public string ResponseCode { get; set; } = string.Empty;
        public string TransactionNo { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string TransactionStatus { get; set; } = string.Empty;
    }
}
