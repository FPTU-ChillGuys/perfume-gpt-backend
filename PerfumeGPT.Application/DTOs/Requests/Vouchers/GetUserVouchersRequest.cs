using PerfumeGPT.Application.DTOs.Requests.Base;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.DTOs.Requests.Vouchers
{
	public class GetUserVouchersRequest : PagingAndSortingQuery
	{
		/// <summary>
		/// Filter by usage status (Available, Reserved, Used)
		/// </summary>
		public UsageStatus? Status { get; set; }

		/// <summary>
		/// Filter by whether the voucher is used or not
		/// </summary>
		public bool? IsUsed { get; set; }

		/// <summary>
		/// Filter to show only expired vouchers
		/// </summary>
		public bool? IsExpired { get; set; }

		/// <summary>
		/// Filter by voucher code (partial match)
		/// </summary>
		public string? Code { get; set; }

		/// <summary>
		/// Filter by discount type (Percentage, FixedAmount)
		/// </summary>
		public DiscountType? DiscountType { get; set; }
	}
}
