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
using System.Linq.Expressions;

namespace PerfumeGPT.Persistence.Repositories
{
	public class PaymentRepository : GenericRepository<PaymentTransaction>, IPaymentRepository
	{
		public PaymentRepository(PerfumeDbContext context) : base(context) { }

		public async Task<PaymentTransactionOverviewResponse> GetTransactionsForManagementAsync(GetPaymentTransactionsFilterRequest request)
		{
			var (fromDateUtc, toDateUtc) = ResolveDateRange(request.FromDate, request.ToDate);

			IQueryable<PaymentTransaction> query = _context.PaymentTransactions
				.AsNoTracking();
			Expression<Func<PaymentTransaction, bool>> filter = pt => pt.CreatedAt >= fromDateUtc && pt.CreatedAt <= toDateUtc;

			if (request.PaymentMethod.HasValue)
			{
				var paymentMethod = request.PaymentMethod.Value;
				filter = filter.AndAlso(pt => pt.Method == paymentMethod);
			}

			if (request.TransactionType.HasValue)
			{
				var transactionType = request.TransactionType.Value;
				filter = filter.AndAlso(pt => pt.TransactionType == transactionType);
			}
			query = query.Where(filter);

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

			var allowedSortColumns = new HashSet<string>(StringComparer.Ordinal)
			{
				nameof(PaymentTransaction.CreatedAt),
				nameof(PaymentTransaction.Amount),
				nameof(PaymentTransaction.Method),
				nameof(PaymentTransaction.TransactionType),
				nameof(PaymentTransaction.TransactionStatus),
				nameof(PaymentTransaction.RetryAttempt),
				nameof(PaymentTransaction.UpdatedAt)
			};
			var sortBy = request.SortBy?.Trim();
			sortBy = !string.IsNullOrWhiteSpace(sortBy)
				? (sortBy.Length == 1
					? char.ToUpper(sortBy[0]).ToString()
					: char.ToUpper(sortBy[0]) + sortBy.Substring(1))
				: null;
			var sortedQuery = !string.IsNullOrWhiteSpace(sortBy) && allowedSortColumns.Contains(sortBy)
				? query.ApplySorting(sortBy, request.IsDescending)
				: query.OrderByDescending(pt => pt.CreatedAt);

			var transactions = await sortedQuery
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
