using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Suppliers;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class SupplierService : ISupplierService
	{
		private readonly ISupplierRepository _supplierRepository;

		public SupplierService(ISupplierRepository supplierRepository)
		{
			_supplierRepository = supplierRepository;
		}

		public async Task<BaseResponse<List<SupplierLookupItem>>> GetSupplierLookupListAsync()
		{
			var suppliers = await _supplierRepository.GetAllAsync(
				asNoTracking: true
			);

			var lookupItems = suppliers
				.OrderBy(s => s.Name)
				.Select(s => new SupplierLookupItem
				{
					Id = s.Id,
					Name = s.Name ?? "Unknown",
					Phone = s.Phone,
					ContactEmail = s.ContactEmail
				})
				.ToList();

			return BaseResponse<List<SupplierLookupItem>>.Ok(lookupItems, "Supplier lookup list retrieved successfully");
		}
	}
}
