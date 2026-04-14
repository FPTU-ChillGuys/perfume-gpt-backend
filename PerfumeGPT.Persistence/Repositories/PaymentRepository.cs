using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Requests.Payments;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Payments;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Persistence.Contexts;
using PerfumeGPT.Persistence.Extensions;
using PerfumeGPT.Persistence.Repositories.Commons;

namespace PerfumeGPT.Persistence.Repositories
{
	public class PaymentRepository : GenericRepository<PaymentTransaction>, IPaymentRepository
	{
		public PaymentRepository(PerfumeDbContext context) : base(context) { }

		public async Task<PaymentTransactionOverviewResponse> GetTransactionsForManagementAsync(GetPaymentTransactionsFilterRequest request)
		{
			var (fromDateUtc, toDateUtc) = ResolveDateRange(request.FromDate, request.ToDate);

			IQueryable<PaymentTransaction> query = _context.PaymentTransactions
				.AsNoTracking()
				.Where(pt => pt.CreatedAt >= fromDateUtc && pt.CreatedAt <= toDateUtc);

			if (request.PaymentMethod.HasValue)
			{
				query = query.Where(pt => pt.Method == request.PaymentMethod.Value);
			}

			if (request.TransactionType.HasValue)
			{
				query = query.Where(pt => pt.TransactionType == request.TransactionType.Value);
			}

			var totalTransactions = await query.CountAsync();
			var totalPaymentTransactions = await query.CountAsync(pt => pt.TransactionType == TransactionType.Payment);
			var totalRefundTransactions = await query.CountAsync(pt => pt.TransactionType == TransactionType.Refund);
			var pendingTransactionsCount = await query.CountAsync(pt => pt.TransactionStatus == TransactionStatus.Pending);
			var successTransactionsCount = await query.CountAsync(pt => pt.TransactionStatus == TransactionStatus.Success);
			var failedTransactionsCount = await query.CountAsync(pt => pt.TransactionStatus == TransactionStatus.Failed);
			var cancelledTransactionsCount = await query.CountAsync(pt => pt.TransactionStatus == TransactionStatus.Cancelled);
			var totalPaymentAmount = await query
				.Where(pt => pt.TransactionType == TransactionType.Payment && pt.TransactionStatus == TransactionStatus.Success)
				.SumAsync(pt => (decimal?)pt.Amount) ?? 0m;

			var successfulPaidOrderIdsQuery = query
				.Where(pt => pt.TransactionType == TransactionType.Payment && pt.TransactionStatus == TransactionStatus.Success)
				.Select(pt => pt.OrderId)
				.Distinct();

			var totalShippingFeeDeductedPerOrder = await _context.Orders
				.AsNoTracking()
				.Where(o => successfulPaidOrderIdsQuery.Contains(o.Id))
				.SumAsync(o => (decimal?)(o.ForwardShipping != null ? o.ForwardShipping.ShippingFee : 0m)) ?? 0m;

			var totalReturnShippingFeeDeducted = await _context.OrderReturnRequests
				.AsNoTracking()
				.Where(r => successfulPaidOrderIdsQuery.Contains(r.OrderId))
				.SumAsync(r => (decimal?)(r.ReturnShipping != null ? r.ReturnShipping.ShippingFee : 0m)) ?? 0m;

			totalShippingFeeDeductedPerOrder += totalReturnShippingFeeDeducted;

			var totalPaymentAmountExcludingShipping = Math.Max(0m, totalPaymentAmount - totalShippingFeeDeductedPerOrder);

			var totalRefundAmount = await query
				.Where(pt => pt.TransactionType == TransactionType.Refund)
				.SumAsync(pt => (decimal?)Math.Abs(pt.Amount)) ?? 0m;

			var sortBy = string.IsNullOrWhiteSpace(request.SortBy)
				? nameof(PaymentTransaction.CreatedAt)
				: request.SortBy;

			var transactions = await query
				  .ApplySorting(sortBy, request.IsDescending)
				  .Skip((request.PageNumber - 1) * request.PageSize)
				  .Take(request.PageSize)
				  .Select(pt => new PaymentTransactionAdminItemResponse
				  {
					  Id = pt.Id,
					  OrderId = pt.OrderId,
					  OrderCode = pt.Order.Code,
					  Method = pt.Method,
					  TransactionType = pt.TransactionType,
					  TransactionStatus = pt.TransactionStatus,
					  Amount = pt.Amount,
					  GatewayTransactionNo = pt.GatewayTransactionNo,
					  FailureReason = pt.FailureReason,
					  OriginalPaymentId = pt.OriginalPaymentId,
					  RetryAttempt = pt.RetryAttempt,
					  CreatedAt = pt.CreatedAt,
					  UpdatedAt = pt.UpdatedAt
				  })
				  .ToListAsync();

			var summary = new PaymentTransactionSummaryResponse
			{
				FromDate = fromDateUtc,
				ToDate = toDateUtc,
				TotalTransactions = totalTransactions,
				TotalPaymentTransactions = totalPaymentTransactions,
				TotalRefundTransactions = totalRefundTransactions,
				PendingTransactionsCount = pendingTransactionsCount,
				SuccessTransactionsCount = successTransactionsCount,
				FailedTransactionsCount = failedTransactionsCount,
				CancelledTransactionsCount = cancelledTransactionsCount,
				TotalPaymentAmount = totalPaymentAmount,
               TotalShippingFeeDeductedPerOrder = totalShippingFeeDeductedPerOrder,
				TotalPaymentAmountExcludingShipping = totalPaymentAmountExcludingShipping,
				TotalRefundAmount = totalRefundAmount
			};

			return new PaymentTransactionOverviewResponse
			{
				Summary = summary,
				Transactions = new PagedResult<PaymentTransactionAdminItemResponse>(
					transactions,
					request.PageNumber,
					request.PageSize,
					totalTransactions)
			};
		}

		private static (DateTime FromDateUtc, DateTime ToDateUtc) ResolveDateRange(DateTime? fromDate, DateTime? toDate)
		{
			var to = toDate?.ToUniversalTime() ?? DateTime.UtcNow;
			var from = fromDate?.ToUniversalTime() ?? to.AddDays(-30);

			if (from > to)
			{
				(from, to) = (to, from);
			}

			return (from, to);
		}
	}
}
