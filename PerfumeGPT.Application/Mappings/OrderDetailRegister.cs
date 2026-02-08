using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Orders;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class OrderDetailRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<OrderDetail, OrderDetailResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.VariantId, src => src.VariantId)
				.Map(dest => dest.VariantName, src => src.ProductVariant != null ? $"{src.ProductVariant.Sku} - {src.ProductVariant.VolumeMl}ml" : string.Empty)
				.Map(dest => dest.ImageUrl, src => src.ProductVariant != null && src.ProductVariant.Media.Count > 0
					? src.ProductVariant.Media.FirstOrDefault(m => m.IsPrimary) != null
						? src.ProductVariant.Media.First(m => m.IsPrimary).Url
						: src.ProductVariant.Media.First().Url
					: null)
				.Map(dest => dest.Quantity, src => src.Quantity)
				.Map(dest => dest.UnitPrice, src => src.UnitPrice)
				.Map(dest => dest.Total, src => src.UnitPrice * src.Quantity);
		}
	}
}
