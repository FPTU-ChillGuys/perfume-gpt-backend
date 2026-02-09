using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;

namespace PerfumeGPT.Application.Services
{
	public class AttributeService : IAttributeService
	{
		private readonly IAttributeRepository _attributeRepository;
		private readonly IProductAttributeRepository _productAttributeRepository;

		public AttributeService(IAttributeRepository attributeRepository, IProductAttributeRepository productAttributeRepository)
		{
			_attributeRepository = attributeRepository;
			_productAttributeRepository = productAttributeRepository;
		}

		public async Task<BaseResponse<List<AttributeLookupItem>>> GetLookupListAsync(bool? isVariantLevel = null)
		{
			var lookupList = await _attributeRepository.GetLookupListAsync(isVariantLevel);
			return BaseResponse<List<AttributeLookupItem>>.Ok(lookupList, "Attribute lookup list retrieved successfully.");
		}

		public async Task<BaseResponse<string>> CreateAttributeAsync(CreateAttributeRequest request)
		{
			if (string.IsNullOrWhiteSpace(request.Name))
				return BaseResponse<string>.Fail("Name is required", ResponseErrorType.BadRequest);

			var entity = new Domain.Entities.Attribute
			{
				Name = request.Name,
				Description = request.Description ?? string.Empty,
				IsVariantLevel = request.IsVariantLevel
			};

			await _attributeRepository.AddAsync(entity);
			var saved = await _attributeRepository.SaveChangesAsync();
			if (!saved) return BaseResponse<string>.Fail("Failed to create attribute", ResponseErrorType.InternalError);

			return BaseResponse<string>.Ok(entity.Id.ToString(), "Attribute created successfully");
		}

		public async Task<BaseResponse<string>> UpdateAttributeAsync(int attributeId, UpdateAttributeRequest request)
		{
			var entity = await _attributeRepository.GetByIdAsync(attributeId);
			if (entity == null) return BaseResponse<string>.Fail("Attribute not found", ResponseErrorType.NotFound);

			if (!string.IsNullOrWhiteSpace(request.Name)) entity.Name = request.Name;
			if (request.Description != null) entity.Description = request.Description;
			if (request.IsVariantLevel.HasValue) entity.IsVariantLevel = request.IsVariantLevel.Value;

			_attributeRepository.Update(entity);
			var saved = await _attributeRepository.SaveChangesAsync();
			if (!saved) return BaseResponse<string>.Fail("Failed to update attribute", ResponseErrorType.InternalError);

			return BaseResponse<string>.Ok(attributeId.ToString(), "Attribute updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteAttributeAsync(int attributeId)
		{
			var entity = await _attributeRepository.GetByIdAsync(attributeId);
			if (entity == null) return BaseResponse<string>.Fail("Attribute not found", ResponseErrorType.NotFound);

			// Check if attribute is used by any product/variant
			if (await _productAttributeRepository.AnyAsync(pa => pa.AttributeId == attributeId))
				return BaseResponse<string>.Fail("Attribute is in use and cannot be deleted", ResponseErrorType.BadRequest);

			_attributeRepository.Remove(entity);
			var saved = await _attributeRepository.SaveChangesAsync();
			if (!saved) return BaseResponse<string>.Fail("Failed to delete attribute", ResponseErrorType.InternalError);

			return BaseResponse<string>.Ok(attributeId.ToString(), "Attribute deleted successfully");
		}
	}
}
