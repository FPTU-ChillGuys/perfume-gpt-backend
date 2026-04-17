using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Address;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;
using static PerfumeGPT.Domain.Entities.Address;

namespace PerfumeGPT.Application.Services
{
	public class AddressService : IAddressService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public AddressService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task<BaseResponse<string>> CreateAddressAsync(Guid userId, CreateAddressRequest request)
		{
			var userAddresses = await _unitOfWork.Addresses.GetUserAddresses(userId);
			var shouldBeDefault = userAddresses.Count == 0 || request.IsDefault;

			if (shouldBeDefault && userAddresses.Count > 0)
			{
				var currentDefault = await _unitOfWork.Addresses.FirstOrDefaultAsync(
					a => a.UserId == userId && a.IsDefault);
				currentDefault?.UnsetDefault();
				if (currentDefault != null) _unitOfWork.Addresses.Update(currentDefault);
			}

			var addressDetails = _mapper.Map<AddressDetails>(request);
			var address = Address.CreateForUser(userId, addressDetails, shouldBeDefault);

			await _unitOfWork.Addresses.AddAsync(address);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Không thể tạo địa chỉ");

			return BaseResponse<string>.Ok(address.Id.ToString(), "Tạo địa chỉ thành công");
		}

		public async Task<BaseResponse<string>> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequest request)
		{
			var address = await _unitOfWork.Addresses.GetByIdAsync(addressId)
				?? throw AppException.NotFound("Không tìm thấy địa chỉ");

			address.EnsureOwnedBy(userId);
			var updatedDetails = _mapper.Map<AddressDetails>(request);
			address.UpdateDetails(updatedDetails);
			_unitOfWork.Addresses.Update(address);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Không thể cập nhật địa chỉ");

			return BaseResponse<string>.Ok(address.Id.ToString(), "Cập nhật địa chỉ thành công");
		}

		public async Task<BaseResponse<string>> DeleteAddressAsync(Guid userId, Guid addressId)
		{
			var address = await _unitOfWork.Addresses.GetByIdAsync(addressId)
				?? throw AppException.NotFound("Không tìm thấy địa chỉ");

			address.EnsureOwnedBy(userId);
			address.EnsureNotAlreadyDefault();

			_unitOfWork.Addresses.Remove(address);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Không thể xóa địa chỉ");

			return BaseResponse<string>.Ok(addressId.ToString(), "Xóa địa chỉ thành công");
		}

		public async Task<BaseResponse<string>> SetDefaultAddressAsync(Guid userId, Guid addressId)
		{
			var address = await _unitOfWork.Addresses.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId)
				?? throw AppException.NotFound("Không tìm thấy địa chỉ hoặc địa chỉ không thuộc về người dùng này");

			address.EnsureNotAlreadyDefault();

			var currentDefault = await _unitOfWork.Addresses.FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);
			currentDefault?.UnsetDefault();
			if (currentDefault != null) _unitOfWork.Addresses.Update(currentDefault);

			address.SetAsDefault();
			_unitOfWork.Addresses.Update(address);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Không thể đặt địa chỉ mặc định");

			return BaseResponse<string>.Ok(addressId.ToString(), "Đặt địa chỉ mặc định thành công");
		}

		public async Task<BaseResponse<AddressResponse>> GetAddressByIdAsync(Guid userId, Guid addressId)
		{
			var address = await _unitOfWork.Addresses.GetUserAddressById(userId, addressId)
				?? throw AppException.NotFound("Không tìm thấy địa chỉ");

			return BaseResponse<AddressResponse>.Ok(address, "Lấy địa chỉ thành công");
		}

		public async Task<BaseResponse<AddressResponse>> GetDefaultAddressAsync(Guid userId)
		{
			var address = await _unitOfWork.Addresses.GetDefaultAddressAsync(userId)
				?? throw AppException.NotFound("Không tìm thấy địa chỉ mặc định");

			return BaseResponse<AddressResponse>.Ok(address, "Lấy địa chỉ mặc định thành công");
		}

		public async Task<BaseResponse<List<AddressResponse>>> GetUserAddressesAsync(Guid userId)
		{
			var addresses = await _unitOfWork.Addresses.GetUserAddresses(userId);

			return BaseResponse<List<AddressResponse>>.Ok(addresses, addresses.Count == 0
			  ? "Không tìm thấy địa chỉ nào"
				: "Lấy danh sách địa chỉ của người dùng thành công");
		}
	}
}
