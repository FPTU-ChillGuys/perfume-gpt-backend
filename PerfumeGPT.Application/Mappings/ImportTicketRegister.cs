using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Imports;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ImportTicketMappingConfig : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			// ImportTicket -> ImportTicketResponse
			config.NewConfig<ImportTicket, ImportTicketResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CreatedById, src => src.CreatedById)
				.Map(dest => dest.CreatedByName, src => src.CreatedByUser != null ? src.CreatedByUser.FullName : "Unknown")
				.Map(dest => dest.SupplierId, src => src.SupplierId)
				.Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : "Unknown")
				.Map(dest => dest.ImportDate, src => src.ImportDate)
				.Map(dest => dest.TotalCost, src => src.TotalCost)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.ImportDetails, src => src.ImportDetails);

			// ImportTicket -> ImportTicketListItem
			config.NewConfig<ImportTicket, ImportTicketListItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CreatedByName, src => src.CreatedByUser != null ? src.CreatedByUser.FullName : "Unknown")
				.Map(dest => dest.SupplierName, src => src.Supplier != null ? src.Supplier.Name : "Unknown")
				.Map(dest => dest.ImportDate, src => src.ImportDate)
				.Map(dest => dest.TotalCost, src => src.TotalCost)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.TotalItems, src => src.ImportDetails.Count)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt);

			// ImportDetail -> ImportDetailResponse
			config.NewConfig<ImportDetail, ImportDetailResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.VariantId, src => src.ProductVariantId)
				.Map(dest => dest.VariantName, src => src.ProductVariant != null && src.ProductVariant.Product != null
					? src.ProductVariant.Product.Name
					: "Unknown")
				.Map(dest => dest.VariantSku, src => src.ProductVariant != null ? src.ProductVariant.Sku : "Unknown")
				.Map(dest => dest.Quantity, src => src.Quantity)
				.Map(dest => dest.UnitPrice, src => src.UnitPrice)
				.Map(dest => dest.TotalPrice, src => src.Quantity * src.UnitPrice)
				.Map(dest => dest.Batches, src => src.Batches);

			// Batch -> BatchResponse
			config.NewConfig<Batch, BatchResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.BatchCode, src => src.BatchCode)
				.Map(dest => dest.ManufactureDate, src => src.ManufactureDate)
				.Map(dest => dest.ExpiryDate, src => src.ExpiryDate)
				.Map(dest => dest.ImportQuantity, src => src.ImportQuantity)
				.Map(dest => dest.RemainingQuantity, src => src.RemainingQuantity)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt);
		}
	}
}
