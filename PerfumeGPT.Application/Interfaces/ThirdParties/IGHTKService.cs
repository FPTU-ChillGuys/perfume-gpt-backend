using PerfumeGPT.Application.DTOs.Requests.Address.GHTKs;
using PerfumeGPT.Application.DTOs.Responses.Address.GHTKs;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IGHTKService
	{
		Task<AddressLevel4Response> GetAddressLevel4Async(AddressLevel4Request request);
	}
}
