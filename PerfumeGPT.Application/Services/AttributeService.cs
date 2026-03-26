using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using Attribute = PerfumeGPT.Domain.Entities.Attribute;

namespace PerfumeGPT.Application.Services
{
	public class AttributeService : IAttributeService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IValidator<CreateAttributeRequest> _createValidator;
		private readonly IValidator<UpdateAttributeRequest> _updateValidator;

		public AttributeService(
			IUnitOfWork unitOfWork,
			IValidator<CreateAttributeRequest> createValidator,
			IValidator<UpdateAttributeRequest> updateValidator)
		{
			_unitOfWork = unitOfWork;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<AttributeLookupItem>>> GetLookupListAsync(bool? isVariantLevel = null)
		{
			var lookupList = await _unitOfWork.Attributes.GetLookupListAsync(isVariantLevel);
			return BaseResponse<List<AttributeLookupItem>>.Ok(
				lookupList,
				"Attribute lookup list retrieved successfully.");
		}

		public async Task<BaseResponse<string>> CreateAttributeAsync(CreateAttributeRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var entity = Attribute.Create(
				request.InternalCode,
				request.Name,
				request.Description,
				request.IsVariantLevel);

			await _unitOfWork.Attributes.AddAsync(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create attribute");

			return BaseResponse<string>.Ok(entity.Id.ToString(), "Attribute created successfully");
		}

		public async Task<BaseResponse<string>> UpdateAttributeAsync(int attributeId, UpdateAttributeRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var entity = await _unitOfWork.Attributes.GetByIdAsync(attributeId)
				 ?? throw AppException.NotFound("Attribute not found");

			entity.Update(request.Name, request.Description, request.IsVariantLevel);
			_unitOfWork.Attributes.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update attribute");

			return BaseResponse<string>.Ok(attributeId.ToString(), "Attribute updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteAttributeAsync(int attributeId)
		{
			var entity = await _unitOfWork.Attributes.GetByIdAsync(attributeId)
				 ?? throw AppException.NotFound("Attribute not found");

			var isInUse = await _unitOfWork.Attributes.IsInUseAsync(attributeId);
			Attribute.EnsureCanBeDeleted(isInUse);

			_unitOfWork.Attributes.Remove(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete attribute");

			return BaseResponse<string>.Ok(attributeId.ToString(), "Attribute deleted successfully");
		}
	}
}
