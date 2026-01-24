using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IUserVoucherRepository : IGenericRepository<UserVoucher>
	{
		/// <summary>
		/// Gets paged user vouchers with voucher details included, with sorting and filtering.
		/// </summary>
		Task<(List<UserVoucher> Items, int TotalCount)> GetPagedWithVouchersAsync(
			Guid userId,
			GetUserVouchersRequest request);

		/// <summary>
		/// Checks if a user has already redeemed a specific voucher.
		/// </summary>
		Task<bool> HasRedeemedVoucherAsync(Guid userId, Guid voucherId);

		/// <summary>
		/// Gets an unused user voucher for a specific voucher ID.
		/// </summary>
		Task<UserVoucher?> GetUnusedUserVoucherAsync(Guid userId, Guid voucherId);
	}
}


