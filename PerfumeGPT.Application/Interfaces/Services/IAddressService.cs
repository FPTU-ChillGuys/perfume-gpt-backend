using PerfumeGPT.Application.DTOs.Requests.Address;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IAddressService
	{
		Task<BaseResponse<AddressResponse>> GetDefaultAddressAsync(Guid userId);
		Task<BaseResponse<List<AddressResponse>>> GetUserAddressesAsync(Guid userId);
		Task<BaseResponse<AddressResponse>> GetAddressByIdAsync(Guid userId, Guid addressId);
		Task<BaseResponse<string>> CreateAddressAsync(Guid userId, CreateAddressRequest request);
		Task<BaseResponse<string>> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequest request);
		Task<BaseResponse<string>> DeleteAddressAsync(Guid userId, Guid addressId);
		Task<BaseResponse<string>> SetDefaultAddressAsync(Guid userId, Guid addressId);
	}
}
