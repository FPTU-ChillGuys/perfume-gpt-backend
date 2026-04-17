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
		private readonly IUnitOfWork _unitOfWork;

		public AttributeValueService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

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
			if (!saved) throw AppException.Internal("Tạo giá trị thuộc tính thất bại");

			return BaseResponse<string>.Ok(entity.Id.ToString(), "Tạo giá trị thuộc tính thành công");
		}

		public async Task<BaseResponse<string>> UpdateAttributeValueAsync(int valueId, UpdateAttributeValueRequest request)
		{
			var entity = await _unitOfWork.AttributeValues.GetByIdAsync(valueId)
				?? throw AppException.NotFound("Không tìm thấy giá trị thuộc tính");

			entity.Update(request.Value);
			_unitOfWork.AttributeValues.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Cập nhật giá trị thuộc tính thất bại");

			return BaseResponse<string>.Ok(valueId.ToString(), "Cập nhật giá trị thuộc tính thành công");
		}

		public async Task<BaseResponse<string>> DeleteAttributeValueAsync(int valueId)
		{
			var entity = await _unitOfWork.AttributeValues.GetByIdAsync(valueId)
			?? throw AppException.NotFound("Không tìm thấy giá trị thuộc tính");

			var isInUse = await _unitOfWork.AttributeValues.IsInUseAsync(valueId);
			if (isInUse)
				throw AppException.Conflict("Không thể xóa giá trị thuộc tính vì đang được sử dụng bởi một hoặc nhiều sản phẩm");

			_unitOfWork.AttributeValues.Remove(entity);
			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Xóa giá trị thuộc tính thất bại");

			return BaseResponse<string>.Ok(valueId.ToString(), "Xóa giá trị thuộc tính thành công");
		}
	}
}
