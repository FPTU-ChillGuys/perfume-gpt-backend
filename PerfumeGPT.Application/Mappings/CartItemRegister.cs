using Mapster;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class CartItemRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<CartItem, GetCartItemResponse>()
				.Map(dest => dest.CartItemId, src => src.Id)
				.Map(dest => dest.VariantId, src => src.VariantId)
				.Map(dest => dest.ImageUrl, src => src.ProductVariant.Media.FirstOrDefault(m => m.IsPrimary).Url ?? string.Empty)
				.Map(dest => dest.VariantName, src => $"{src.ProductVariant.Product.Name} - {src.ProductVariant.VolumeMl}ml")
				.Map(dest => dest.VolumeMl, src => src.ProductVariant.VolumeMl)
				.Map(dest => dest.VariantPrice, src => src.ProductVariant.BasePrice)
				.Map(dest => dest.Quantity, src => src.Quantity);
		}
	}
}

