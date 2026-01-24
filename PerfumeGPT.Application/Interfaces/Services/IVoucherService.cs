using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IVoucherService
	{
		// Admin operations
		Task<BaseResponse<string>> CreateVoucherAsync(CreateVoucherRequest request);
		Task<BaseResponse<string>> UpdateVoucherAsync(Guid voucherId, UpdateVoucherRequest request);
		Task<BaseResponse<string>> DeleteVoucherAsync(Guid voucherId);
		Task<BaseResponse<VoucherResponse>> GetVoucherAsync(Guid voucherId);
		Task<BaseResponse<PagedResult<VoucherResponse>>> GetVouchersAsync(GetPagedVouchersRequest request);

		// User operations
		Task<Voucher?> GetVoucherByCodeAsync(string code);
		Task<BaseResponse<string>> RedeemVoucherAsync(Guid userId, RedeemVoucherRequest request);
		Task<BaseResponse<PagedResult<UserVoucherResponse>>> GetUserVouchersAsync(Guid userId, GetUserVouchersRequest request);

		// Apply voucher logic
		Task<BaseResponse<ApplyVoucherResponse>> ApplyVoucherToOrderAsync(Guid userId, ApplyVoucherRequest request);
		Task<BaseResponse<bool>> ValidateToApplyVoucherAsync(Guid voucherId, Guid userId);

		// Voucher status management
		Task<BaseResponse<bool>> MarkVoucherAsReservedAsync(Guid userId, Guid voucherId);
		Task<BaseResponse<bool>> MarkVoucherAsUsedAsync(Guid userId, Guid voucherId);
		Task<BaseResponse<bool>> ReleaseReservedVoucherAsync(Guid userId, Guid voucherId);

		/// <summary>
		/// Calculates the discount amount for a voucher given the total price.
		/// Returns the final price after discount is applied.
		/// </summary>
		/// <param name="voucherId">The voucher ID to apply</param>
		/// <param name="totalPrice">The total price before discount</param>
		/// <returns>The final price after discount, or original price if voucher is invalid</returns>
		Task<decimal> CalculateVoucherDiscountAsync(Guid voucherId, decimal totalPrice);
	}
}

