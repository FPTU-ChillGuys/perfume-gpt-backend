using Mapster;
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

		public SupplierService(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
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
				?? throw AppException.NotFound("Supplier not found");

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
				throw AppException.Conflict("Supplier name already exists.");

			var emailExists = await _unitOfWork.Suppliers.AnyAsync(s => s.ContactEmail.ToUpper() == normalizedEmail);
			if (emailExists)
				throw AppException.Conflict("Supplier contact email already exists.");

			var entity = Supplier.Create(request.Name, request.ContactEmail, request.Phone, request.Address);
			await _unitOfWork.Suppliers.AddAsync(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create supplier");

			return BaseResponse<SupplierResponse>.Ok(entity.Adapt<SupplierResponse>());
		}

		public async Task<BaseResponse<SupplierResponse>> UpdateSupplierAsync(int id, UpdateSupplierRequest request)
		{
			var entity = await _unitOfWork.Suppliers.GetByIdAsync(id)
				?? throw AppException.NotFound("Supplier not found");

			var normalizedName = Supplier.NormalizeName(request.Name).ToUpperInvariant();
			var normalizedEmail = Supplier.NormalizeEmail(request.ContactEmail).ToUpperInvariant();

			var nameExists = await _unitOfWork.Suppliers.AnyAsync(s => s.Id != id && s.Name.ToUpper() == normalizedName);
			if (nameExists)
				throw AppException.Conflict("Supplier name already exists.");

			var emailExists = await _unitOfWork.Suppliers.AnyAsync(s => s.Id != id && s.ContactEmail.ToUpper() == normalizedEmail);
			if (emailExists)
				throw AppException.Conflict("Supplier contact email already exists.");

			entity.UpdateDetails(request.Name, request.ContactEmail, request.Phone, request.Address);
			_unitOfWork.Suppliers.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update supplier");

			return BaseResponse<SupplierResponse>.Ok(entity.Adapt<SupplierResponse>());
		}

		public async Task<BaseResponse<bool>> DeleteSupplierAsync(int id)
		{
			var entity = await _unitOfWork.Suppliers.GetByIdAsync(id)
				?? throw AppException.NotFound("Supplier not found");

			var hasImportTickets = await _unitOfWork.Suppliers.HasImportTicketsAsync(id);
			Supplier.EnsureCanBeDeleted(hasImportTickets);

			_unitOfWork.Suppliers.Remove(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete supplier");

			return BaseResponse<bool>.Ok(true);
		}
	}
}
