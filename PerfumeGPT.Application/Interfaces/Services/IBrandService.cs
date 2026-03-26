using PerfumeGPT.Application.DTOs.Requests.Metadatas.Brands;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Metadatas.Brands;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IBrandService
	{
		Task<BaseResponse<List<BrandLookupItem>>> GetBrandLookupAsync();
		Task<BaseResponse<BrandResponse>> GetBrandByIdAsync(int id);
		Task<BaseResponse<List<BrandResponse>>> GetAllBrandsAsync();
		Task<BaseResponse<BrandResponse>> CreateBrandAsync(CreateBrandRequest request);
		Task<BaseResponse<BrandResponse>> UpdateBrandAsync(int id, UpdateBrandRequest request);
		Task<BaseResponse<bool>> DeleteBrandAsync(int id);
	}
}
