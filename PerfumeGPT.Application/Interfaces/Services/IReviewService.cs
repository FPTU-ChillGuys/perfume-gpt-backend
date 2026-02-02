using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Reviews;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IReviewService
	{
		Task<BaseResponse<BulkActionResult<Guid>>> CreateReviewAsync(Guid userId, CreateReviewRequest request);
		Task<BaseResponse<BulkActionResult<string>>> UpdateReviewAsync(Guid userId, Guid reviewId, UpdateReviewRequest request);
		Task<BaseResponse<string>> DeleteReviewAsync(Guid userId, Guid reviewId);
		Task<BaseResponse<string>> ModerateReviewAsync(Guid staffId, Guid reviewId, ModerateReviewRequest request);
		Task<BaseResponse<PagedResult<ReviewListItem>>> GetReviewsAsync(GetPagedReviewsRequest request);
		Task<BaseResponse<ReviewDetailResponse>> GetReviewByIdAsync(Guid reviewId);
		Task<BaseResponse<List<ReviewResponse>>> GetUserReviewsAsync(Guid userId);
		Task<BaseResponse<List<ReviewResponse>>> GetVariantReviewsAsync(Guid variantId);
		Task<BaseResponse<ReviewStatisticsResponse>> GetVariantReviewStatisticsAsync(Guid variantId);
		Task<BaseResponse<PagedResult<ReviewListItem>>> GetPendingReviewsAsync(int pageNumber = 1, int pageSize = 10);
		Task<BaseResponse<bool>> CanUserReviewOrderDetailAsync(Guid userId, Guid orderDetailId);
		Task<BaseResponse<List<MediaResponse>>> GetReviewImagesAsync(Guid reviewId);
	}
}
