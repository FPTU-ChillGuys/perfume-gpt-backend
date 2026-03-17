using System;

namespace PerfumeGPT.Application.DTOs.Requests.VNPays
{
    public class VnPayRefundRequest
    {
        public Guid OrderId { get; set; }
        public decimal Amount { get; set; }
        public Guid PaymentId { get; set; }
        public string TransactionType { get; set; } = "02"; // 02 for full refund, 03 for partial
        public string CreateBy { get; set; } = string.Empty;
        public string OrderInfo { get; set; } = string.Empty;
        public string TransactionDate { get; set; } = string.Empty;
    }
}
