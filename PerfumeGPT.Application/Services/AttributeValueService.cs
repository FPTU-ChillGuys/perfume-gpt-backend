using PerfumeGPT.Application.DTOs.Requests.ProductAttributes;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.ProductAttributes.Values;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using AttributeValue = PerfumeGPT.Domain.Entities.AttributeValue;

namespace PerfumeGPT.Application.Services
{
	public class AttributeValueService : IAttributeValueService
	{
		#region Depedencies
		private readonly IUnitOfWork _unitOfWork;

		public AttributeValueService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<AttributeValueLookupItem>>> GetLookupListByAttributeIdAsync(int attributeId)
		{
			var values = await _unitOfWork.AttributeValues.GetLookupListByAttributeIdAsync(attributeId);
			return BaseResponse<List<AttributeValueLookupItem>>.Ok(values);
		}

		public async Task<BaseResponse<string>> CreateAttributeValueAsync(int attributeId, CreateAttributeValueRequest request)
		{
			var entity = AttributeValue.Create(attributeId, request.Value);

			await _unitOfWork.AttributeValues.AddAsync(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create attribute value");

			return BaseResponse<string>.Ok(entity.Id.ToString(), "Attribute value created successfully");
		}

		public async Task<BaseResponse<string>> UpdateAttributeValueAsync(int valueId, UpdateAttributeValueRequest request)
		{
			var entity = await _unitOfWork.AttributeValues.GetByIdAsync(valueId)
				?? throw AppException.NotFound("Attribute value not found");

			entity.Update(request.Value);
			_unitOfWork.AttributeValues.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update attribute value");

			return BaseResponse<string>.Ok(valueId.ToString(), "Attribute value updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteAttributeValueAsync(int valueId)
		{
			var entity = await _unitOfWork.AttributeValues.GetByIdAsync(valueId)
			?? throw AppException.NotFound("Attribute value not found");

			var isInUse = await _unitOfWork.AttributeValues.IsInUseAsync(valueId);
			AttributeValue.EnsureCanBeDeleted(isInUse);

			_unitOfWork.AttributeValues.Remove(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete attribute value");

			return BaseResponse<string>.Ok(valueId.ToString(), "Attribute value deleted successfully");
		}
	}
}
