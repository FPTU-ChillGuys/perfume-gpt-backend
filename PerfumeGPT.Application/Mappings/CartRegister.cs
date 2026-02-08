using Mapster;
using PerfumeGPT.Application.DTOs.Responses.CartItems;
using PerfumeGPT.Application.DTOs.Responses.Carts;

namespace PerfumeGPT.Application.Mappings
{
	public class CartRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<List<GetCartItemResponse>, GetCartItemsResponse>()
				.Map(dest => dest.Items, src => src);
		}
	}
}
