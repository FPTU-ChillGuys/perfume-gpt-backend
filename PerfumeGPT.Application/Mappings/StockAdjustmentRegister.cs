using Mapster;
using PerfumeGPT.Application.DTOs.Responses.StockAdjustments;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class StockAdjustmentMappingConfig : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<StockAdjustment, StockAdjustmentResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CreatedById, src => src.CreatedById)
				.Map(dest => dest.CreatedByName, src => src.CreatedByUser.FullName ?? "Unknown")
				.Map(dest => dest.VerifiedById, src => src.VerifiedById)
				.Map(dest => dest.VerifiedByName, src => src.VerifiedByUser != null ? src.VerifiedByUser.FullName : null)
				.Map(dest => dest.AdjustmentDate, src => src.AdjustmentDate)
				.Map(dest => dest.Reason, src => src.Reason)
				.Map(dest => dest.Note, src => src.Note)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.UpdatedAt, src => src.UpdatedAt)
				.Map(dest => dest.AdjustmentDetails, src => src.AdjustmentDetails);

			config.NewConfig<StockAdjustment, StockAdjustmentListItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CreatedByName, src => src.CreatedByUser != null ? src.CreatedByUser.FullName : "Unknown")
				.Map(dest => dest.AdjustmentDate, src => src.AdjustmentDate)
				.Map(dest => dest.Reason, src => src.Reason)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.TotalItems, src => src.AdjustmentDetails.Count)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt);

			config.NewConfig<StockAdjustmentDetail, StockAdjustmentDetailResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.ProductVariantId, src => src.ProductVariantId)
				.Map(dest => dest.ProductName, src => src.ProductVariant.Product.Name ?? "Unknown")
				.Map(dest => dest.VariantSku, src => src.ProductVariant.Sku ?? "Unknown")
				.Map(dest => dest.BatchId, src => src.BatchId)
				.Map(dest => dest.BatchCode, src => src.Batch.BatchCode ?? "Unknown")
				.Map(dest => dest.AdjustmentQuantity, src => src.AdjustmentQuantity)
				.Map(dest => dest.ApprovedQuantity, src => src.ApprovedQuantity)
				.Map(dest => dest.Note, src => src.Note);
		}
	}
}
