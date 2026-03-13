using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class RecipientService : IRecipientService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IAddressService _addressService;
		private readonly IValidator<RecipientInformation> _validator;
		private readonly IMapper _mapper;

		public RecipientService(IUnitOfWork unitOfWork, IAddressService addressService, IValidator<RecipientInformation> validator, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_addressService = addressService;
			_validator = validator;
			_mapper = mapper;
		}

		public async Task<BaseResponse<RecipientInformation>> ResolveRecipientDataAsync(
			RecipientInformation? recipientInfo,
			Guid? savedAddressId,
			Guid? customerId)
		{
			// 1) If request includes AddressId -> must have customerId and we load saved address
			if (savedAddressId.HasValue == true)
			{
				if (!customerId.HasValue)
				{
					return BaseResponse<RecipientInformation>.Fail("Customer ID required when using saved address.", ResponseErrorType.BadRequest);
				}

				var savedAddress = await _unitOfWork.Addresses.GetUserAddressById(customerId.Value, savedAddressId.Value);
				if (savedAddress == null)
				{
					return BaseResponse<RecipientInformation>.Fail("Saved address not found.", ResponseErrorType.NotFound);
				}

				return BaseResponse<RecipientInformation>.Ok(_mapper.Map<RecipientInformation>(savedAddress));
			}

			// 2) If request provided without AddressId -> validate and use it
			if (recipientInfo != null)
			{
				var validationResult = await _validator.ValidateAsync(recipientInfo);
				if (!validationResult.IsValid)
				{
					var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
					return BaseResponse<RecipientInformation>.Fail(
						"Recipient information validation failed.",
						ResponseErrorType.BadRequest,
						errors);
				}

				return BaseResponse<RecipientInformation>.Ok(recipientInfo);
			}

			// 3) No request -> try customer's default address if available
			if (customerId.HasValue)
			{
				var customerAddress = await _addressService.GetDefaultAddressAsync(customerId.Value);
				if (!customerAddress.Success || customerAddress.Payload == null)
				{
					return BaseResponse<RecipientInformation>.Fail(
						"Customer default address not found. Please provide recipient information.",
						customerAddress.ErrorType,
						customerAddress.Errors);
				}

				var response = _mapper.Map<RecipientInformation>(customerAddress.Payload);
				return BaseResponse<RecipientInformation>.Ok(response);
			}

			// 4) Nothing provided
			return BaseResponse<RecipientInformation>.Fail(
				"Either recipient information or customer ID must be provided.");
		}

		public async Task<BaseResponse<RecipientInfo>> CreateRecipientInfoAsync(
			Guid orderId,
			RecipientInformation? request,
			Guid? savedAddressId = null,
			Guid? customerId = null)
		{
			var resolvedData = await ResolveRecipientDataAsync(request, savedAddressId, customerId);
			if (!resolvedData.Success)
			{
				return BaseResponse<RecipientInfo>.Fail(resolvedData.Message, resolvedData.ErrorType, resolvedData.Errors);
			}

			var recipientData = resolvedData.Payload!;
			var recipientInfo = new RecipientInfo
			{
				OrderId = orderId,
				FullName = recipientData.FullName,
				Phone = recipientData.Phone,
				DistrictId = recipientData.DistrictId,
				DistrictName = recipientData.DistrictName,
				WardCode = recipientData.WardCode,
				WardName = recipientData.WardName,
				ProvinceName = recipientData.ProvinceName,
				FullAddress = recipientData.FullAddress
			};

			await _unitOfWork.RecipientInfos.AddAsync(recipientInfo);
			return BaseResponse<RecipientInfo>.Ok(recipientInfo);
		}

		public async Task<BaseResponse<RecipientInfo>> UpdateRecipientInfoAsync(
			RecipientInfo existingRecipient,
			RecipientInformation? request,
			Guid? savedAddressId,
			Guid userId)
		{
			var resolvedData = await ResolveRecipientDataAsync(request, savedAddressId, userId);
			if (!resolvedData.Success)
			{
				return BaseResponse<RecipientInfo>.Fail(resolvedData.Message, resolvedData.ErrorType, resolvedData.Errors);
			}

			var recipientData = resolvedData.Payload!;

			existingRecipient.FullName = recipientData.FullName;
			existingRecipient.Phone = recipientData.Phone;
			existingRecipient.DistrictId = recipientData.DistrictId;
			existingRecipient.DistrictName = recipientData.DistrictName;
			existingRecipient.WardCode = recipientData.WardCode;
			existingRecipient.WardName = recipientData.WardName;
			existingRecipient.ProvinceName = recipientData.ProvinceName;
			existingRecipient.FullAddress = recipientData.FullAddress;

			_unitOfWork.RecipientInfos.Update(existingRecipient);
			return BaseResponse<RecipientInfo>.Ok(existingRecipient);
		}
	}
}