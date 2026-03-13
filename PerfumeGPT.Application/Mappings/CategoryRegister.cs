using Mapster;
using PerfumeGPT.Application.DTOs.Requests.Categories;
using PerfumeGPT.Application.DTOs.Responses.Categories;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class CategoryRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Category, CategoriesLookupItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name);

			config.NewConfig<Category, CategoryResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.Name, src => src.Name);

			config.NewConfig<CreateCategoryRequest, Category>()
				.Map(dest => dest.Name, src => src.Name);

			config.NewConfig<UpdateCategoryRequest, Category>()
				.Map(dest => dest.Name, src => src.Name);
		}
	}
}
