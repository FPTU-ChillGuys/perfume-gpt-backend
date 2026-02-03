using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ImportTicketMappingConfig : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<ImportTicket, ImportTicketResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CreatedById, src => src.CreatedById)
				.Map(dest => dest.CreatedByName, src => src.CreatedByUser.FullName ?? "Unknown")
				.Map(dest => dest.VerifiedById, src => src.VerifiedById)
				.Map(dest => dest.VerifiedByName, src => src.VerifiedByUser != null ? src.VerifiedByUser.FullName : null)
				.Map(dest => dest.SupplierId, src => src.SupplierId)
				.Map(dest => dest.SupplierName, src => src.Supplier.Name ?? "Unknown")
				.Map(dest => dest.ImportDate, src => src.ImportDate)
				.Map(dest => dest.TotalCost, src => src.TotalCost)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.ImportDetails, src => src.ImportDetails);

		config.NewConfig<ImportTicket, ImportTicketListItem>()
			.Map(dest => dest.Id, src => src.Id)
			.Map(dest => dest.CreatedByName, src => src.CreatedByUser != null ? src.CreatedByUser.FullName : "Unknown")
			.Map(dest => dest.VerifiedByName, src => src.VerifiedByUser != null ? src.VerifiedByUser.FullName : null)
			.Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : "Unknown")
			.Map(dest => dest.ImportDate, src => src.ImportDate)
			.Map(dest => dest.TotalCost, src => src.TotalCost)
			.Map(dest => dest.Status, src => src.Status)
			.Map(dest => dest.TotalItems, src => src.ImportDetails.Count)
			.Map(dest => dest.CreatedAt, src => src.CreatedAt);
		}
	}
}
