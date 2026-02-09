using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Values;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class AttributeValueService : IAttributeValueService
	{
		private readonly IAttributeValueRepository _attributeValueRepo;
		private readonly IProductAttributeRepository _productAttributeRepository;

		public AttributeValueService(IAttributeValueRepository attributeValueRepo, IProductAttributeRepository productAttributeRepository)
		{
			_attributeValueRepo = attributeValueRepo;
			_productAttributeRepository = productAttributeRepository;
		}

		public async Task<BaseResponse<List<AttributeValueLookupItem>>> GetLookupListByAttributeIdAsync(int attributeId)
		{
			var values = await _attributeValueRepo.GetLookupListByAttributeIdAsync(attributeId);
			return BaseResponse<List<AttributeValueLookupItem>>.Ok(values);
		}

		public async Task<BaseResponse<string>> CreateAttributeValueAsync(CreateAttributeValueRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Value)) return BaseResponse<string>.Fail("Value is required", ResponseErrorType.BadRequest);

			var entity = new AttributeValue
			{
				AttributeId = request.AttributeId,
				Value = request.Value
			};

			await _attributeValueRepo.AddAsync(entity);
			var saved = await _attributeValueRepo.SaveChangesAsync();
			if (!saved) return BaseResponse<string>.Fail("Failed to create attribute value", ResponseErrorType.InternalError);

			return BaseResponse<string>.Ok(entity.Id.ToString(), "Attribute value created successfully");
		}

		public async Task<BaseResponse<string>> UpdateAttributeValueAsync(int valueId, UpdateAttributeValueRequest request)
		{
			var entity = await _attributeValueRepo.GetByIdAsync(valueId);
			if (entity == null) return BaseResponse<string>.Fail("Attribute value not found", ResponseErrorType.NotFound);

			if (request.Value != null) entity.Value = request.Value;

			_attributeValueRepo.Update(entity);
			var saved = await _attributeValueRepo.SaveChangesAsync();
			if (!saved) return BaseResponse<string>.Fail("Failed to update attribute value", ResponseErrorType.InternalError);

			return BaseResponse<string>.Ok(valueId.ToString(), "Attribute value updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteAttributeValueAsync(int valueId)
		{
			var entity = await _attributeValueRepo.GetByIdAsync(valueId);
			if (entity == null) return BaseResponse<string>.Fail("Attribute value not found", ResponseErrorType.NotFound);

			// Check if attribute value is used by any product/variant
			if (await _productAttributeRepository.AnyAsync(pa => pa.ValueId == valueId))
				return BaseResponse<string>.Fail("Attribute value is in use and cannot be deleted", ResponseErrorType.BadRequest);

			_attributeValueRepo.Remove(entity);
			var saved = await _attributeValueRepo.SaveChangesAsync();
			if (!saved) return BaseResponse<string>.Fail("Failed to delete attribute value", ResponseErrorType.InternalError);

			return BaseResponse<string>.Ok(valueId.ToString(), "Attribute value deleted successfully");
		}
	}
}
