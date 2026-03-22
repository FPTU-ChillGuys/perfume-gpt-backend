using FluentValidation;
using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using Attribute = PerfumeGPT.Domain.Entities.Attribute;

namespace PerfumeGPT.Application.Services
{
	public class AttributeService : IAttributeService
	{
		#region Dependencies
		private readonly IAttributeRepository _attributeRepository;
		private readonly IProductAttributeRepository _productAttributeRepository;
		private readonly IValidator<CreateAttributeRequest> _createValidator;
		private readonly IValidator<UpdateAttributeRequest> _updateValidator;
		private readonly IMapper _mapper;

		public AttributeService(
			IAttributeRepository attributeRepository,
			IProductAttributeRepository productAttributeRepository,
			IValidator<CreateAttributeRequest> createValidator,
			IValidator<UpdateAttributeRequest> updateValidator,
			IMapper mapper)
		{
			_attributeRepository = attributeRepository;
			_productAttributeRepository = productAttributeRepository;
			_createValidator = createValidator;
			_updateValidator = updateValidator;
			_mapper = mapper;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<AttributeLookupItem>>> GetLookupListAsync(bool? isVariantLevel = null)
		{
			var lookupList = await _attributeRepository.GetLookupListAsync(isVariantLevel);
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

			var entity = _mapper.Map<Attribute>(request);
			await _attributeRepository.AddAsync(entity);

			var saved = await _attributeRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create attribute");

			return BaseResponse<string>.Ok(entity.Id.ToString(), "Attribute created successfully");
		}

		public async Task<BaseResponse<string>> UpdateAttributeAsync(int attributeId, UpdateAttributeRequest request)
		{
			var validationResult = await _updateValidator.ValidateAsync(request);
			if (!validationResult.IsValid)
				throw AppException.BadRequest("Validation failed",
					validationResult.Errors.Select(e => e.ErrorMessage).ToList());

			var entity = await _attributeRepository.GetByIdAsync(attributeId)
				?? throw AppException.NotFound("Attribute not found");

			_mapper.Map(request, entity);
			_attributeRepository.Update(entity);

			var saved = await _attributeRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update attribute");

			return BaseResponse<string>.Ok(attributeId.ToString(), "Attribute updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteAttributeAsync(int attributeId)
		{
			var entity = await _attributeRepository.GetByIdAsync(attributeId)
				?? throw AppException.NotFound("Attribute not found");

			var isInUse = await _productAttributeRepository.AnyAsync(pa => pa.AttributeId == attributeId);
			Attribute.EnsureCanBeDeleted(isInUse);

			_attributeRepository.Remove(entity);
			var saved = await _attributeRepository.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete attribute");

			return BaseResponse<string>.Ok(attributeId.ToString(), "Attribute deleted successfully");
		}
	}
}
