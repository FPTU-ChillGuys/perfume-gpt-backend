using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Address;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class AddressService : IAddressService
	{
		#region Dependencies
		private readonly IAddressRepository _addressRepo;
		private readonly IValidator<CreateAddressRequest> _createValidator;
		private readonly IValidator<UpdateAddressRequest> _updateValidator;

		public AddressService(
			IAddressRepository addressRepo,
			IValidator<CreateAddressRequest> validator,
			IValidator<UpdateAddressRequest> updateValidator)
		{
			_addressRepo = addressRepo;
			_createValidator = validator;
			_updateValidator = updateValidator;
		}
		#endregion Dependencies

		public async Task<BaseResponse<string>> CreateAddressAsync(Guid userId, CreateAddressRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var userAddresses = await _addressRepo.GetUserAddresses(userId);
			var shouldBeDefault = userAddresses.Count == 0 || request.IsDefault;

			if (shouldBeDefault && userAddresses.Count > 0)
			{
				var currentDefault = await _addressRepo.FirstOrDefaultAsync(
					a => a.UserId == userId && a.IsDefault);
				currentDefault?.UnsetDefault();
				if (currentDefault != null) _addressRepo.Update(currentDefault);
			}

			var address = Address.CreateForUser(
				userId,
				request.RecipientName,
				request.RecipientPhoneNumber,
				request.Street,
				request.Ward,
				request.District,
				request.City,
				request.WardCode,
				request.DistrictId,
				request.ProvinceId,
				shouldBeDefault);

			await _addressRepo.AddAsync(address);
			var saved = await _addressRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not create address");

			return BaseResponse<string>.Ok(address.Id.ToString(), "Address created successfully");
		}

		public async Task<BaseResponse<string>> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					validationResult.Errors.Select(e => e.ErrorMessage).ToList());

			var address = await _addressRepo.GetByIdAsync(addressId)
				?? throw AppException.NotFound("Address not found");

			address.EnsureOwnedBy(userId);
			address.UpdateDetails(
				  request.RecipientName,
				  request.RecipientPhoneNumber,
				  request.Street,
				  request.Ward,
				  request.District,
				  request.City,
				  request.WardCode,
				  request.DistrictId,
				  request.ProvinceId);
			_addressRepo.Update(address);

			var saved = await _addressRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not update address");

			return BaseResponse<string>.Ok(address.Id.ToString(), "Address updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteAddressAsync(Guid userId, Guid addressId)
		{
			var address = await _addressRepo.GetByIdAsync(addressId)
				?? throw AppException.NotFound("Address not found");

			address.EnsureOwnedBy(userId);
			address.EnsureCanBeDeleted();

			_addressRepo.Remove(address);
			var saved = await _addressRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not delete address");

			return BaseResponse<string>.Ok(addressId.ToString(), "Address deleted successfully");
		}

		public async Task<BaseResponse<string>> SetDefaultAddressAsync(Guid userId, Guid addressId)
		{
			var address = await _addressRepo.FirstOrDefaultAsync(a => a.Id == addressId && a.UserId == userId)
				?? throw AppException.NotFound("Address not found or does not belong to this user");

			address.EnsureNotAlreadyDefault();

			var currentDefault = await _addressRepo.FirstOrDefaultAsync(a => a.UserId == userId && a.IsDefault);
			currentDefault?.UnsetDefault();
			if (currentDefault != null) _addressRepo.Update(currentDefault);

			address.SetAsDefault();
			_addressRepo.Update(address);

			var saved = await _addressRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Could not set default address");

			return BaseResponse<string>.Ok(addressId.ToString(), "Default address set successfully");
		}

		public async Task<BaseResponse<AddressResponse>> GetAddressByIdAsync(Guid userId, Guid addressId)
		{
			var address = await _addressRepo.GetUserAddressById(userId, addressId)
				?? throw AppException.NotFound("Address not found");

			return BaseResponse<AddressResponse>.Ok(address, "Address retrieved successfully");
		}

		public async Task<BaseResponse<AddressResponse>> GetDefaultAddressAsync(Guid userId)
		{
			var address = await _addressRepo.GetDefaultAddress(userId)
				?? throw AppException.NotFound("Default address not found");

			return BaseResponse<AddressResponse>.Ok(address, "Default address retrieved successfully");
		}

		public async Task<BaseResponse<List<AddressResponse>>> GetUserAddressesAsync(Guid userId)
		{
			var addresses = await _addressRepo.GetUserAddresses(userId);

			return BaseResponse<List<AddressResponse>>.Ok(addresses, addresses.Count == 0
				? "No addresses found"
				: "User addresses retrieved successfully");
		}
	}
}
