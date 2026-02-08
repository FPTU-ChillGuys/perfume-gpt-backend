using Mapster;
using PerfumeGPT.Application.DTOs.Responses.StockAdjustments;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class AdjustmentDetailRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{

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
