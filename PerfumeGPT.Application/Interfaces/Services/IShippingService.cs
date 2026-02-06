namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IShippingService
	{
		Task<decimal?> CalculateShippingFeeAsync(int districtId, string wardCode);
	}
}
