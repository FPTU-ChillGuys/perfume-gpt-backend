using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Values;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using AttributeValue = PerfumeGPT.Domain.Entities.AttributeValue;

namespace PerfumeGPT.Application.Services
{
	public class AttributeValueService : IAttributeValueService
	{
		#region Depedencies
		private readonly IAttributeValueRepository _attributeValueRepo;
		private readonly IProductAttributeRepository _productAttributeRepository;
		private readonly IValidator<CreateAttributeValueRequest> _createValidator;
		private readonly IValidator<UpdateAttributeValueRequest> _updateValidator;
		private readonly IMapper _mapper;

		public AttributeValueService(
			  IAttributeValueRepository attributeValueRepo,
			  IProductAttributeRepository productAttributeRepository,
			  IValidator<CreateAttributeValueRequest> createValidator,
			  IValidator<UpdateAttributeValueRequest> updateValidator,
			  IMapper mapper)
		{
			_attributeValueRepo = attributeValueRepo;
			_productAttributeRepository = productAttributeRepository;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_mapper = mapper;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<AttributeValueLookupItem>>> GetLookupListByAttributeIdAsync(int attributeId)
		{
			var values = await _attributeValueRepo.GetLookupListByAttributeIdAsync(attributeId);
			return BaseResponse<List<AttributeValueLookupItem>>.Ok(values);
		}

		public async Task<BaseResponse<string>> CreateAttributeValueAsync(int attributeId, CreateAttributeValueRequest request)
		{
			var validationResult = await _createValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var entity = _mapper.Map<AttributeValue>(request);
			entity.AttributeId = attributeId;

			await _attributeValueRepo.AddAsync(entity);
			var saved = await _attributeValueRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create attribute value");

			return BaseResponse<string>.Ok(entity.Id.ToString(), "Attribute value created successfully");
		}

		public async Task<BaseResponse<string>> UpdateAttributeValueAsync(int valueId, UpdateAttributeValueRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					[.. validationResult.Errors.Select(e => e.ErrorMessage)]);

			var entity = await _attributeValueRepo.GetByIdAsync(valueId) ?? throw AppException.NotFound("Attribute value not found");
			_mapper.Map(request, entity);

			_attributeValueRepo.Update(entity);
			var saved = await _attributeValueRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update attribute value");

			return BaseResponse<string>.Ok(valueId.ToString(), "Attribute value updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteAttributeValueAsync(int valueId)
		{
			var entity = await _attributeValueRepo.GetByIdAsync(valueId) ?? throw AppException.NotFound("Attribute value not found");

			// Check if attribute value is used by any product/variant
			var isInUse = await _productAttributeRepository.AnyAsync(pa => pa.AttributeId == valueId);
			AttributeValue.EnsureCanBeDeleted(isInUse);

			_attributeValueRepo.Remove(entity);
			var saved = await _attributeValueRepo.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete attribute value");

			return BaseResponse<string>.Ok(valueId.ToString(), "Attribute value deleted successfully");
		}
	}
}
