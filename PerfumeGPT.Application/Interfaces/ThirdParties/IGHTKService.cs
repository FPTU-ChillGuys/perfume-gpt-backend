using PerfumeGPT.Application.DTOs.Requests.Address.GHTKs;
using PerfumeGPT.Application.DTOs.Responses.Address.GHTKs;
using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Interfaces.ThirdParties
{
	public interface IGHTKService
	{
		Task<BaseResponse<AddressLevel4Response>> GetAddressLevel4Async(GetAddressLevel4Request request);
	}
}
