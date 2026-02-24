using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Brands;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IBrandService
	{
		Task<BaseResponse<List<BrandLookupItem>>> GetBrandLookupAsync();
	}
}
