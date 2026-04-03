using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ShippingInfoRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<ShippingInfo, ShippingInfoResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.CarrierName, src => src.CarrierName)
				.Map(dest => dest.TrackingNumber, src => src.TrackingNumber)
				.Map(dest => dest.ShippingFee, src => src.ShippingFee)
				.Map(dest => dest.Status, src => src.Status)
				.Map(dest => dest.EstimatedDeliveryDate, src => src.EstimatedDeliveryDate)
				.Map(dest => dest.ShippedDate, src => src.ShippedDate);
		}
	}
}
