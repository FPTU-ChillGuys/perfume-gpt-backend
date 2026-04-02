using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class RecipientService : IRecipientService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IValidator<RecipientInformation> _validator;
		private readonly IMapper _mapper;

		public RecipientService(IUnitOfWork unitOfWork, IValidator<RecipientInformation> validator, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_validator = validator;
			_mapper = mapper;
		}
		#endregion Dependencies

		public async Task<RecipientInformation> ResolveRecipientDataAsync(RecipientInformation? recipientInfo, Guid? savedAddressId, Guid? customerId)
		{
			// If request includes AddressId -> must have customerId and we load saved address
			if (savedAddressId.HasValue == true)
			{
				if (!customerId.HasValue)
					throw AppException.BadRequest("Customer ID required when using saved address.");

				var savedAddress = await _unitOfWork.Addresses.GetUserAddressById(customerId.Value, savedAddressId.Value);
				return savedAddress == null
					? throw AppException.NotFound("Saved address not found.")
					: _mapper.Map<RecipientInformation>(savedAddress);
			}

			// If request provided without AddressId -> validate and use it
			if (recipientInfo != null)
			{
				var validationResult = await _validator.ValidateAsync(recipientInfo);
				if (!validationResult.IsValid)
				{
					var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
					throw AppException.BadRequest("Recipient information validation failed.", errors);
				}

				return recipientInfo;
			}

			// Try customer's default address if available
			if (customerId.HasValue)
			{
				var customerAddress = await _unitOfWork.Addresses.GetDefaultAddressAsync(customerId.Value)
					?? throw AppException.NotFound("No default address found for customer.");

				var response = _mapper.Map<RecipientInformation>(customerAddress);
				return response;
			}

			// Nothing provided
			throw AppException.BadRequest("Either recipient information or customer ID must be provided.");
		}

		public async Task<RecipientInfo> CreateRecipientInfoAsync(Guid orderId, RecipientInformation? request, Guid? savedAddressId = null, Guid? customerId = null)
		{
			var recipientData = await ResolveRecipientDataAsync(request, savedAddressId, customerId);

			var payload = _mapper.Map<RecipientInfo.RecipientPayload>(recipientData);
			var recipientInfo = RecipientInfo.Create(orderId, payload);

			await _unitOfWork.RecipientInfos.AddAsync(recipientInfo);
			return recipientInfo;
		}

		public async Task<RecipientInfo> UpdateRecipientInfoAsync(
			RecipientInfo existingRecipient,
			RecipientInformation? request,
			Guid? savedAddressId,
			Guid userId)
		{
			var recipientData = await ResolveRecipientDataAsync(request, savedAddressId, userId);

			var payload = _mapper.Map<RecipientInfo.RecipientPayload>(recipientData);
			existingRecipient.UpdateRecipient(payload);

			_unitOfWork.RecipientInfos.Update(existingRecipient);
			return existingRecipient;
		}
	}
}