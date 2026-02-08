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
		}
	}
}
