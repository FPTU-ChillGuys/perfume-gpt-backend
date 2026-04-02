using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Attributes;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using static PerfumeGPT.Domain.Entities.Attribute;
using Attribute = PerfumeGPT.Domain.Entities.Attribute;

namespace PerfumeGPT.Application.Services
{
	public class AttributeService : IAttributeService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public AttributeService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}

		public async Task<BaseResponse<List<AttributeLookupItem>>> GetLookupListAsync(bool? isVariantLevel = null)
		{
			var lookupList = await _unitOfWork.Attributes.GetLookupListAsync(isVariantLevel);
			return BaseResponse<List<AttributeLookupItem>>.Ok(
				lookupList,
				"Attribute lookup list retrieved successfully.");
		}

		public async Task<BaseResponse<string>> CreateAttributeAsync(CreateAttributeRequest request)
		{
			var details = _mapper.Map<AttributeCreationDetails>(request);
			var entity = Attribute.Create(details);
			await _unitOfWork.Attributes.AddAsync(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create attribute");

			return BaseResponse<string>.Ok(entity.Id.ToString(), "Attribute created successfully");
		}

		public async Task<BaseResponse<string>> UpdateAttributeAsync(int attributeId, UpdateAttributeRequest request)
		{
			var entity = await _unitOfWork.Attributes.GetByIdAsync(attributeId)
				 ?? throw AppException.NotFound("Attribute not found");

			var details = _mapper.Map<AttributeUpdateDetails>(request);
			entity.Update(details);
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
			if (isInUse)
				throw AppException.Conflict("Attribute is in use and cannot be deleted.");

			_unitOfWork.Attributes.Remove(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete attribute");

			return BaseResponse<string>.Ok(attributeId.ToString(), "Attribute deleted successfully");
		}
	}
}
