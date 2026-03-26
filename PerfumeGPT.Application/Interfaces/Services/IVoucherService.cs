using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Vouchers;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IVoucherService
	{
		// Admin operations
		Task<BaseResponse<string>> CreateRegularVoucherAsync(CreateVoucherRequest request);
		Task<BaseResponse<string>> UpdateVoucherAsync(Guid voucherId, UpdateVoucherRequest request);
		Task<BaseResponse<string>> DeleteVoucherAsync(Guid voucherId);
		Task<BaseResponse<VoucherResponse>> GetVoucherByIdAsync(Guid voucherId);
		Task<BaseResponse<PagedResult<VoucherResponse>>> GetPagedVouchersAsync(GetPagedVouchersRequest request);

		// User operations
		Task<VoucherResponse?> GetVoucherByCodeAsync(string code);
		Task<BaseResponse<string>> RedeemVoucherAsync(Guid userId, RedeemVoucherRequest request);
		Task<BaseResponse<PagedResult<UserVoucherResponse>>> GetUserVouchersAsync(Guid userId, GetPagedUserVouchersRequest request);
		Task<BaseResponse<PagedResult<RedeemableVoucherResponse>>> GetRedeemableVouchersAsync(GetPagedRedeemableVouchersRequest request);

		// Apply voucher logic
		Task<BaseResponse<PagedResult<AvailableVoucherResponse>>> GetAvailableVouchersAsync(Guid userId, GetPagedAvailableVouchersRequest request);
		Task<bool> CanUserApplyVoucherAsync(string voucherCode, Guid? userId, decimal orderAmount, string? emailOrPhone = null, IEnumerable<Guid>? cartVariantIds = null);

		// Voucher status management
		Task<UserVoucher> MarkVoucherAsReservedAsync(Guid? userId, string? emailOrPhone, Guid voucherId, Guid orderId);
		Task<bool> MarkVoucherAsUsedAsync(Guid orderId);
		Task<bool> ReleaseReservedVoucherAsync(Guid orderId);

		Task<decimal> CalculateVoucherDiscountAsync(string voucherCode, decimal totalPrice);
	}
}

