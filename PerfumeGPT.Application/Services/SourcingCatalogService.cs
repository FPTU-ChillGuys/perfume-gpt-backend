using PerfumeGPT.Application.DTOs.Requests.SourcingCatalogs;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.SourcingCatalogs;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Services
{
	public class SourcingCatalogService : ISourcingCatalogService
	{
		private readonly IUnitOfWork _unitOfWork;

		public SourcingCatalogService(IUnitOfWork unitOfWork) { _unitOfWork = unitOfWork; }

		public async Task<BaseResponse<IEnumerable<CatalogItemResponse>>> GetCatalogsAsync(int? supplierId, Guid? variantId)
		{
			var items = await _unitOfWork.VariantSuppliers.GetCatalogsAsync(supplierId, variantId);
			return BaseResponse<IEnumerable<CatalogItemResponse>>.Ok(items, "Sourcing catalogs retrieved successfully");
		}

		public async Task<BaseResponse<string>> CreateItemAsync(CreateCatalogItemRequest request)
		{
			var variant = await _unitOfWork.Variants.GetByIdAsync(request.ProductVariantId)
				?? throw AppException.NotFound("Variant not found");

			variant.EnsureNotDeleted();

			var supplierExists = await _unitOfWork.Suppliers.AnyAsync(s => s.Id == request.SupplierId);
			if (!supplierExists)
				throw AppException.NotFound("Supplier not found");

			var currentItems = await _unitOfWork.VariantSuppliers.GetByVariantIdAsync(request.ProductVariantId);
			if (currentItems.Any(x => x.SupplierId == request.SupplierId))
				throw AppException.Conflict("Catalog item already exists for this variant and supplier.");

			var shouldBePrimary = request.IsPrimary || currentItems.Count == 0;
			if (shouldBePrimary)
			{
				foreach (var item in currentItems.Where(x => x.IsPrimary))
				{
					item.RemovePrimary();
				}
			}

			var entity = VariantSupplier.Create(
				request.ProductVariantId,
				request.SupplierId,
				request.NegotiatedPrice,
				request.EstimatedLeadTimeDays,
				shouldBePrimary);

			await _unitOfWork.VariantSuppliers.AddAsync(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to create sourcing catalog item");

			return BaseResponse<string>.Ok(entity.Id.ToString(), "Sourcing catalog item created successfully");
		}

		public async Task<BaseResponse<string>> UpdateItemAsync(Guid id, UpdateCatalogItemRequest request)
		{
			var entity = await _unitOfWork.VariantSuppliers.GetByIdAsync(id)
				?? throw AppException.NotFound("Sourcing catalog item not found");

			entity.UpdatePricing(request.NegotiatedPrice, request.EstimatedLeadTimeDays);
			_unitOfWork.VariantSuppliers.Update(entity);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to update sourcing catalog item");

			return BaseResponse<string>.Ok(id.ToString(), "Sourcing catalog item updated successfully");
		}

		public async Task<BaseResponse<string>> SetAsPrimaryAsync(Guid id)
		{
			var target = await _unitOfWork.VariantSuppliers.GetByIdAsync(id)
				?? throw AppException.NotFound("Sourcing catalog item not found");

			var sameVariantItems = await _unitOfWork.VariantSuppliers.GetByVariantIdAsync(target.ProductVariantId);
			foreach (var item in sameVariantItems)
			{
				if (item.Id == id)
					item.SetAsPrimary();
				else
					item.RemovePrimary();
			}

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to set sourcing catalog primary item");

			return BaseResponse<string>.Ok(id.ToString(), "Primary sourcing catalog item updated successfully");
		}

		public async Task<BaseResponse<string>> DeleteItemAsync(Guid id)
		{
			var target = await _unitOfWork.VariantSuppliers.GetByIdAsync(id)
				?? throw AppException.NotFound("Sourcing catalog item not found");

			var sameVariantItems = await _unitOfWork.VariantSuppliers.GetByVariantIdAsync(target.ProductVariantId);
			var nextPrimary = target.IsPrimary
				? sameVariantItems.FirstOrDefault(x => x.Id != target.Id)
				: null;

			_unitOfWork.VariantSuppliers.Remove(target);

			nextPrimary?.SetAsPrimary();

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved) throw AppException.Internal("Failed to delete sourcing catalog item");

			return BaseResponse<string>.Ok(id.ToString(), "Sourcing catalog item deleted successfully");
		}
	}
}
