using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IShippingService
	{
		/// <summary>
		/// Calculates shipping fee for the given district and ward.
		/// </summary>
		/// <param name="districtId">The destination district ID</param>
		/// <param name="wardCode">The destination ward code</param>
		/// <returns>Shipping fee or null if calculation fails</returns>
		Task<decimal?> CalculateShippingFeeAsync(int districtId, string wardCode);
	}
}
