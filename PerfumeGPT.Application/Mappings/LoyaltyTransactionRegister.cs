using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Loyalty;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class LoyaltyTransactionRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<LoyaltyTransaction, LoyaltyTransactionHistoryItemResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.UserId, src => src.UserId)
				.Map(dest => dest.VoucherId, src => src.VoucherId)
				.Map(dest => dest.OrderId, src => src.OrderId)
				.Map(dest => dest.TransactionType, src => src.TransactionType)
				.Map(dest => dest.PointsChanged, src => src.PointsChanged)
				.Map(dest => dest.AbsolutePoints, src => src.PointsChanged < 0 ? -src.PointsChanged : src.PointsChanged)
				.Map(dest => dest.Reason, src => src.Reason);
		}
	}
}
