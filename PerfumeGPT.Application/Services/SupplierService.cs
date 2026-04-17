using MapsterMapper;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Suppliers;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Suppliers;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class SupplierService : ISupplierService
	{
		#region Dependencies
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;

		public SupplierService(IUnitOfWork unitOfWork, IMapper mapper)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
		}
		#endregion Dependencies

		public async Task<BaseResponse<List<SupplierLookupItem>>> GetSupplierLookupListAsync()
		{
			var suppliers = await _unitOfWork.Suppliers.GetSupplierLookupListAsync();
			return BaseResponse<List<SupplierLookupItem>>.Ok(suppliers);
		}

		public async Task<BaseResponse<SupplierResponse>> GetSupplierByIdAsync(int id)
		{
			var supplier = await _unitOfWork.Suppliers.GetSupplierByIdAsync(id)
			   ?? throw AppException.NotFound("Không tìm thấy nhà cung cấp");

			return BaseResponse<SupplierResponse>.Ok(supplier);
		}

		public async Task<BaseResponse<List<SupplierResponse>>> GetAllSuppliersAsync()
		{
			var suppliers = await _unitOfWork.Suppliers.GetAllSuppliersAsync();
			return BaseResponse<List<SupplierResponse>>.Ok(suppliers);
		}

		public async Task<BaseResponse<SupplierResponse>> CreateSupplierAsync(CreateSupplierRequest request)
		{
			var normalizedName = Supplier.NormalizeName(request.Name).ToUpperInvariant();
			var normalizedEmail = Supplier.NormalizeEmail(request.ContactEmail).ToUpperInvariant();

			var nameExists = await _unitOfWork.Suppliers.AnyAsync(s => s.Name.ToUpper() == normalizedName);
			if (nameExists)
				throw AppException.Conflict("Tên nhà cung cấp đã tồn tại.");

			var emailExists = await _unitOfWork.Suppliers.AnyAsync(s => s.ContactEmail.ToUpper() == normalizedEmail);
			if (emailExists)
				throw AppException.Conflict("Email liên hệ của nhà cung cấp đã tồn tại.");

			var payload = _mapper.Map<Supplier.SupplierPayload>(request);
			var entity = Supplier.Create(payload);
			await _unitOfWork.Suppliers.AddAsync(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Tạo nhà cung cấp thất bại");

			return BaseResponse<SupplierResponse>.Ok(_mapper.Map<SupplierResponse>(entity));
		}

		public async Task<BaseResponse<SupplierResponse>> UpdateSupplierAsync(int id, UpdateSupplierRequest request)
		{
			var entity = await _unitOfWork.Suppliers.GetByIdAsync(id)
			   ?? throw AppException.NotFound("Không tìm thấy nhà cung cấp");

			var normalizedName = Supplier.NormalizeName(request.Name).ToUpperInvariant();
			var normalizedEmail = Supplier.NormalizeEmail(request.ContactEmail).ToUpperInvariant();

			var nameExists = await _unitOfWork.Suppliers.AnyAsync(s => s.Id != id && s.Name.ToUpper() == normalizedName);
			if (nameExists)
				throw AppException.Conflict("Tên nhà cung cấp đã tồn tại.");

			var emailExists = await _unitOfWork.Suppliers.AnyAsync(s => s.Id != id && s.ContactEmail.ToUpper() == normalizedEmail);
			if (emailExists)
				throw AppException.Conflict("Email liên hệ của nhà cung cấp đã tồn tại.");

			var payload = _mapper.Map<Supplier.SupplierPayload>(request);
			entity.UpdateDetails(payload);
			_unitOfWork.Suppliers.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Cập nhật nhà cung cấp thất bại");

			return BaseResponse<SupplierResponse>.Ok(_mapper.Map<SupplierResponse>(entity));
		}

		public async Task<BaseResponse<bool>> DeleteSupplierAsync(int id)
		{
			var entity = await _unitOfWork.Suppliers.GetByIdAsync(id)
			   ?? throw AppException.NotFound("Không tìm thấy nhà cung cấp");

			var hasImportTickets = await _unitOfWork.Suppliers.HasImportTicketsAsync(id);
			if (!hasImportTickets) throw AppException.Conflict("Không thể xóa nhà cung cấp có liên kết phiếu nhập.");

			_unitOfWork.Suppliers.Remove(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Xóa nhà cung cấp thất bại");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
