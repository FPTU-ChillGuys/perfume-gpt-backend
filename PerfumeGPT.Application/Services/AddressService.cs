using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Address;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class AddressService : IAddressService
	{
		private readonly IAddressRepository _addressRepo;
		private readonly IValidator<CreateAddressRequest> _validator;
		private readonly IValidator<UpdateAddressRequest> _updateValidator;

		public AddressService(IAddressRepository addressRepo, IValidator<CreateAddressRequest> validator, IValidator<UpdateAddressRequest> updateValidator)
		{
			_addressRepo = addressRepo;
			_validator = validator;
			_updateValidator = updateValidator;
		}

		public async Task<BaseResponse<string>> CreateAddressAsync(Guid userId, CreateAddressRequest request)
		{
			var validationResult = await _validator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed", ResponseErrorType.BadRequest, errors);
			}

			try
			{
				// Check if user has any addresses - if not, make this the default
				var userAddresses = await _addressRepo.GetAllAsync(filter: a => a.UserId == userId);
				var isFirstAddress = !userAddresses.Any();

				var address = new Domain.Entities.Address
				{
					UserId = userId,
					ReceiverName = request.ReceiverName,
					Phone = request.Phone,
					Street = request.Street,
					Ward = request.Ward,
					District = request.District,
					City = request.City,
					WardCode = request.WardCode,
					DistrictId = request.DistrictId,
					ProvinceId = request.ProvinceId,
					IsDefault = isFirstAddress, // First address is automatically default
				};

				await _addressRepo.AddAsync(address);
				var saved = await _addressRepo.SaveChangesAsync();
				if (!saved)
				{
					return BaseResponse<string>.Fail("Could not create address", ResponseErrorType.InternalError);
				}

				return BaseResponse<string>.Ok(address.Id.ToString(), "Address created successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error creating address: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<string>> DeleteAddressAsync(Guid userId, Guid addressId)
		{
			try
			{
				var address = await _addressRepo.GetByIdAsync(addressId);
				if (address == null)
				{
					return BaseResponse<string>.Fail("Address not found", ResponseErrorType.NotFound);
				}

				if (address.UserId != userId)
				{
					return BaseResponse<string>.Fail("Address does not belong to this user", ResponseErrorType.Forbidden);
				}

				if (address.IsDefault)
				{
					return BaseResponse<string>.Fail("Cannot delete default address. Please set another address as default first.", ResponseErrorType.BadRequest);
				}

				_addressRepo.Remove(address);
				var saved = await _addressRepo.SaveChangesAsync();
				if (!saved)
				{
					return BaseResponse<string>.Fail("Could not delete address", ResponseErrorType.InternalError);
				}

				return BaseResponse<string>.Ok(addressId.ToString(), "Address deleted successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error deleting address: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<List<AddressResponse>>> GetUserAddressesAsync(Guid userId)
		{
			var addresses = await _addressRepo.GetUserAddressesWithDetails(userId);
			if (addresses == null)
			{
				return BaseResponse<List<AddressResponse>>.Fail("No addresses found for user", ResponseErrorType.NotFound);
			}
			return BaseResponse<List<AddressResponse>>.Ok(addresses, "User addresses retrieved successfully");
		}

		public async Task<BaseResponse<string>> UpdateAddressAsync(Guid userId, Guid addressId, UpdateAddressRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				return BaseResponse<string>.Fail("Validation failed", ResponseErrorType.BadRequest, errors);
			}

			try
			{
				var address = await _addressRepo.GetByIdAsync(addressId);
				if (address == null)
				{
					return BaseResponse<string>.Fail("Address not found", ResponseErrorType.NotFound);
				}

				if (address.UserId != userId)
				{
					return BaseResponse<string>.Fail("Address does not belong to this user", ResponseErrorType.Forbidden);
				}

				// Update address fields
				address.ReceiverName = request.ReceiverName;
				address.Phone = request.Phone;
				address.Street = request.Street;
				address.Ward = request.Ward;
				address.District = request.District;
				address.City = request.City;
				address.WardCode = request.WardCode;
				address.DistrictId = request.DistrictId;
				address.ProvinceId = request.ProvinceId;

				_addressRepo.Update(address);
				var saved = await _addressRepo.SaveChangesAsync();
				if (!saved)
				{
					return BaseResponse<string>.Fail("Could not update address", ResponseErrorType.InternalError);
				}

				return BaseResponse<string>.Ok(address.Id.ToString(), "Address updated successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error updating address: {ex.Message}", ResponseErrorType.InternalError);
			}
		}
	}
}
