using PerfumeGPT.Application.DTOs.Requests.Vouchers;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Repositories
{
	public interface IVoucherRepository : IGenericRepository<Voucher>
	{
		/// <summary>
		/// Checks if a voucher code exists (excluding a specific voucher ID).
		/// </summary>
		Task<bool> CodeExistsAsync(string code, Guid? excludeVoucherId = null);

		/// <summary>
		/// Gets paged vouchers with filtering by expiration and code.
		/// </summary>
		Task<(List<Voucher> Items, int TotalCount)> GetPagedVouchersAsync(GetPagedVouchersRequest request);
	}
}


