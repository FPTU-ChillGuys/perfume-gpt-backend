using Mapster;

namespace PerfumeGPT.Application.Mappings
{
	public class BrandRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Domain.Entities.Brand, DTOs.Responses.Metadatas.Brands.BrandLookupItem>()
				.MapToConstructor(true);
		}
	}
}
