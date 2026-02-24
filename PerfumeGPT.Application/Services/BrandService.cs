using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Brands;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Services;

namespace PerfumeGPT.Application.Services
{
	public class BrandService : IBrandService
	{
		private readonly IBrandRepository brandRepository;

		public BrandService(IBrandRepository brandRepository)
		{
			this.brandRepository = brandRepository;
		}

		public async Task<BaseResponse<List<BrandLookupItem>>> GetBrandLookupAsync()
		{
			return BaseResponse<List<BrandLookupItem>>.Ok(await brandRepository.GetBrandLookupAsync());
		}
	}
}
