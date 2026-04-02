using System;

namespace PerfumeGPT.Application.DTOs.Responses.VNPays
{
    public record VnPayRefundResponse
    {
        public bool IsSuccess { get; init; }
        public required string Message { get; init; }

        public Guid PaymentId { get; init; }
        public required string ResponseCode { get; init; }
        public required string TransactionNo { get; init; }
        public decimal Amount { get; init; }
        public required string TransactionStatus { get; init; }
    }
}
