using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class ContactAddressService : IContactAddressService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IValidator<ContactAddressInformation> _validator;
		private readonly IMapper _mapper;

		public ContactAddressService(IUnitOfWork unitOfWork, IValidator<ContactAddressInformation> validator, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_validator = validator;
			_mapper = mapper;
		}
		#endregion Dependencies

		public async Task<ContactAddressInformation> ResolveContactAddressDataAsync(ContactAddressInformation? contactAddressInfo, Guid? savedAddressId, Guid? customerId)
		{
			// If request includes AddressId -> must have customerId and we load saved address
			if (savedAddressId.HasValue == true)
			{
				if (!customerId.HasValue)
					throw AppException.BadRequest("Bắt buộc có Customer ID khi dùng địa chỉ đã lưu.");

				var savedAddress = await _unitOfWork.Addresses.GetUserAddressById(customerId.Value, savedAddressId.Value);
				return savedAddress == null
				   ? throw AppException.NotFound("Không tìm thấy địa chỉ đã lưu.")
					: _mapper.Map<ContactAddressInformation>(savedAddress);
			}

			// If request provided without AddressId -> validate and use it
			if (contactAddressInfo != null)
			{
				var validationResult = await _validator.ValidateAsync(contactAddressInfo);
				if (!validationResult.IsValid)
				{
					var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
					throw AppException.BadRequest("Xác thực thông tin địa chỉ liên hệ thất bại.", errors);
				}

				return contactAddressInfo;
			}

			// Try customer's default address if available
			if (customerId.HasValue)
			{
				var customerAddress = await _unitOfWork.Addresses.GetDefaultAddressAsync(customerId.Value)
				   ?? throw AppException.NotFound("Không tìm thấy địa chỉ mặc định của khách hàng.");

				var response = _mapper.Map<ContactAddressInformation>(customerAddress);
				return response;
			}

			// Nothing provided
			throw AppException.BadRequest("Cần cung cấp thông tin địa chỉ liên hệ hoặc Customer ID.");
		}

		public async Task<ContactAddress> CreateContactAddressAsync(ContactAddressInformation? request, Guid? savedAddressId = null, Guid? customerId = null)
		{
			var contactAddressData = await ResolveContactAddressDataAsync(request, savedAddressId, customerId);

			var payload = _mapper.Map<ContactAddress.ContactAddressPayload>(contactAddressData);
			var contactAddress = ContactAddress.Create(payload);

			await _unitOfWork.ContactAddresses.AddAsync(contactAddress);
			return contactAddress;
		}

		public async Task<ContactAddress> UpdateContactAddressAsync(
			  ContactAddress existingContactAddress,
			  ContactAddressInformation? request,
			  Guid? savedAddressId,
			  Guid userId)
		{
			var contactAddressData = await ResolveContactAddressDataAsync(request, savedAddressId, userId);

			var payload = _mapper.Map<ContactAddress.ContactAddressPayload>(contactAddressData);
			existingContactAddress.UpdateContactAddress(payload);

			_unitOfWork.ContactAddresses.Update(existingContactAddress);
			return existingContactAddress;
		}
	}
}