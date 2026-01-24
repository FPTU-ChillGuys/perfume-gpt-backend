using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Address;
using PerfumeGPT.Application.DTOs.Responses.Address;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class AddressService : IAddressService
	{
		private readonly IAddressRepository _addressRepo;
		private readonly IValidator<CreateAddressRequest> _validator;
		private readonly IValidator<UpdateAddressRequest> _updateValidator;
		private readonly IMapper _mapper;

		public AddressService(IAddressRepository addressRepo, IValidator<CreateAddressRequest> validator, IValidator<UpdateAddressRequest> updateValidator, IMapper mapper)
		{
			_addressRepo = addressRepo;
			_validator = validator;
			_updateValidator = updateValidator;
			_mapper = mapper;
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
				var userAddresses = await _addressRepo.GetAllAsync(filter: a => a.UserId == userId);
				var isFirstAddress = !userAddresses.Any();

				var address = _mapper.Map<Address>(request);
				address.UserId = userId;
				address.IsDefault = isFirstAddress;

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

		public async Task<BaseResponse<AddressResponse>> GetDefaultAddressAsync(Guid userId)
		{
			try
			{
				var address = await _addressRepo.GetDefaultAddressWithDetails(userId);
				if (address == null)
				{
					return BaseResponse<AddressResponse>.Fail("Default address not found for user", ResponseErrorType.NotFound);
				}

				var response = _mapper.Map<AddressResponse>(address);
				return BaseResponse<AddressResponse>.Ok(response, "Default address retrieved successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<AddressResponse>.Fail($"Error retrieving default address: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<List<AddressResponse>>> GetUserAddressesAsync(Guid userId)
		{
			try
			{
				var addresses = await _addressRepo.GetUserAddressesWithDetails(userId);
				if (addresses == null || addresses.Count == 0)
				{
					return BaseResponse<List<AddressResponse>>.Fail("No addresses found for user", ResponseErrorType.NotFound);
				}

				var response = _mapper.Map<List<AddressResponse>>(addresses);
				return BaseResponse<List<AddressResponse>>.Ok(response, "User addresses retrieved successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<List<AddressResponse>>.Fail($"Error retrieving user addresses: {ex.Message}", ResponseErrorType.InternalError);
			}
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

				_mapper.Map(request, address);

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

		public async Task<BaseResponse<string>> SetDefaultAddressAsync(Guid userId, Guid addressId)
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
					return BaseResponse<string>.Ok(addressId.ToString(), "Address is already the default");
				}

				var currentDefaultAddress = await _addressRepo.GetDefaultAddressWithDetails(userId);
				if (currentDefaultAddress != null)
				{
					var currentDefault = await _addressRepo.GetByIdAsync(currentDefaultAddress.Id);
					if (currentDefault != null)
					{
						currentDefault.IsDefault = false;
						_addressRepo.Update(currentDefault);
					}
				}

				address.IsDefault = true;
				_addressRepo.Update(address);

				var saved = await _addressRepo.SaveChangesAsync();
				if (!saved)
				{
					return BaseResponse<string>.Fail("Could not set default address", ResponseErrorType.InternalError);
				}

				return BaseResponse<string>.Ok(addressId.ToString(), "Default address set successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<string>.Fail($"Error setting default address: {ex.Message}", ResponseErrorType.InternalError);
			}
		}

		public async Task<BaseResponse<AddressResponse>> GetAddressByIdAsync(Guid userId, Guid addressId)
		{
			try
			{
				var address = await _addressRepo.GetAddressByIdWithDetails(userId, addressId);
				if (address == null)
				{
					return BaseResponse<AddressResponse>.Fail("Address not found", ResponseErrorType.NotFound);
				}

				var response = _mapper.Map<AddressResponse>(address);
				return BaseResponse<AddressResponse>.Ok(response, "Address retrieved successfully");
			}
			catch (Exception ex)
			{
				return BaseResponse<AddressResponse>.Fail($"Error retrieving address: {ex.Message}", ResponseErrorType.InternalError);
			}
		}
	}
}
