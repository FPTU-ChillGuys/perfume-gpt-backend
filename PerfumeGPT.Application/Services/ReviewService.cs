using PerfumeGPT.Application.DTOs.Requests.Reviews;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.DTOs.Responses.Media;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Services.Helpers;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class ReviewService : IReviewService
	{
		#region Dependencies
		private readonly IMediaService _mediaService;
		private readonly IUnitOfWork _unitOfWork;

		private readonly MediaBulkActionHelper _helper;
		private readonly IRedisPublisherService _redisPublisherService;

		public ReviewService(
			IMediaService mediaService,
			MediaBulkActionHelper helper,
			IUnitOfWork unitOfWork,
			IRedisPublisherService redisPublisherService)
		{
			_mediaService = mediaService;
			_helper = helper;
			_unitOfWork = unitOfWork;
			_redisPublisherService = redisPublisherService;
		}
		#endregion Dependencies



		public async Task<BaseResponse<BulkActionResult<Guid>>> CreateReviewAsync(Guid userId, CreateReviewRequest request)
		{
			var canReview = await _unitOfWork.Reviews.CanUserReviewOrderDetailAsync(userId, request.OrderDetailId);
			if (!canReview)
			{
				throw AppException.BadRequest("Bạn không thể đánh giá sản phẩm này. Có thể bạn chưa mua, đơn hàng chưa được giao, hoặc bạn đã đánh giá trước đó.");
			}

			var review = Review.Create(userId, request.OrderDetailId, request.Rating, request.Comment);

            await _unitOfWork.Reviews.AddAsync(review);
			var saved = await _unitOfWork.SaveChangesAsync();

			if (!saved)
				throw AppException.Internal("Tạo đánh giá thất bại");

            // Notify external services via Redis
            var variantId = await _unitOfWork.Reviews.GetVariantIdByOrderDetailIdAsync(request.OrderDetailId);
            await _redisPublisherService.PublishReviewCreatedAsync(variantId);

            var metadata = new BulkActionMetadata { Operations = [] };
			if (request.TemporaryMediaIds != null && request.TemporaryMediaIds.Count != 0)
			{
				var conversionResult = await _helper.ConvertTemporaryMediaToPermanentAsync(request.TemporaryMediaIds, EntityType.Review, review.Id);
				if (conversionResult.TotalProcessed > 0)
				{
					metadata.Operations.Add(BulkOperationResult.FromBulkActionResponse("Media Upload", conversionResult));
				}
			}

			var result = new BulkActionResult<Guid>(review.Id, metadata.Operations.Count > 0 ? metadata : null);
			var message = metadata.HasPartialFailure
			 ? $"Gửi đánh giá thành công nhưng có {metadata.TotalFailed} tệp media tải lên thất bại."
				: "Gửi đánh giá thành công.";

			return BaseResponse<BulkActionResult<Guid>>.Ok(result, message);
		}

		public async Task<BaseResponse<string>> DeleteReviewAsync(Guid userId, Guid reviewId, bool canDeleteAny = false)
		{
			var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId) ?? throw AppException.NotFound("Không tìm thấy đánh giá");

			if (!canDeleteAny && !review.IsAuthor(userId))
				throw AppException.Forbidden("Bạn không có quyền xóa đánh giá này");

			_unitOfWork.Reviews.Remove(review);

			await _mediaService.DeleteAllMediaByEntityAsync(EntityType.Review, reviewId);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Xóa đánh giá thất bại");

			return BaseResponse<string>.Ok("Xóa đánh giá thành công");
		}

		public async Task<BaseResponse<string>> AnswerReviewAsync(Guid reviewId, Guid staffId, AnswerReviewRequest request)
		{
			var review = await _unitOfWork.Reviews.GetByIdAsync(reviewId) ?? throw AppException.NotFound("Không tìm thấy đánh giá");

			if (review.HasStaffResponse())
				throw AppException.BadRequest("Đánh giá này đã có phản hồi từ nhân viên.");

			review.AnswerByStaff(staffId, request.StaffFeedbackComment, DateTime.UtcNow);

			var saved = await _unitOfWork.SaveChangesAsync();
			if (!saved)
				throw AppException.Internal("Phản hồi đánh giá thất bại");

			return BaseResponse<string>.Ok("Phản hồi đánh giá thành công");
		}

		public async Task<BaseResponse<PagedResult<ReviewListItem>>> GetReviewsAsync(GetPagedReviewsRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Reviews.GetPagedReviewsAsync(request);

			var pagedResult = new PagedResult<ReviewListItem>(
				items,
				request.PageNumber,
				request.PageSize,
				totalCount
			);

			return BaseResponse<PagedResult<ReviewListItem>>.Ok(pagedResult);
		}

		public async Task<BaseResponse<ReviewDetailResponse>> GetReviewByIdAsync(Guid reviewId)
		{
			var response = await _unitOfWork.Reviews.GetReviewWithDetailsAsync(reviewId) ?? throw AppException.NotFound("Không tìm thấy đánh giá");
			return BaseResponse<ReviewDetailResponse>.Ok(response);
		}

		public async Task<BaseResponse<List<ReviewResponse>>> GetUserReviewsAsync(Guid userId)
		{
			var response = await _unitOfWork.Reviews.GetReviewsByUserIdAsync(userId);
			return BaseResponse<List<ReviewResponse>>.Ok(response);
		}

		public async Task<BaseResponse<List<ReviewResponse>>> GetVariantReviewsAsync(Guid variantId)
		{
			var response = await _unitOfWork.Reviews.GetReviewsByVariantIdAsync(variantId);
			return BaseResponse<List<ReviewResponse>>.Ok(response);
		}

		public async Task<BaseResponse<ReviewStatisticsResponse>> GetVariantReviewStatisticsAsync(Guid variantId)
		{
			var (totalReviews, averageRating, starCounts) = await _unitOfWork.Reviews.GetVariantReviewStatisticsAsync(variantId);

			var response = new ReviewStatisticsResponse
			{
				VariantId = variantId,
				TotalReviews = totalReviews,
				AverageRating = averageRating,
				FiveStarCount = starCounts[4],
				FourStarCount = starCounts[3],
				ThreeStarCount = starCounts[2],
				TwoStarCount = starCounts[1],
				OneStarCount = starCounts[0]
			};

			return BaseResponse<ReviewStatisticsResponse>.Ok(response);
		}

		public async Task<BaseResponse<List<MediaResponse>>> GetReviewImagesAsync(Guid reviewId)
		{
			var existed = await _unitOfWork.Reviews.AnyAsync(rv => rv.Id == reviewId);
			if (!existed) throw AppException.NotFound("Không tìm thấy đánh giá");
			return await _mediaService.GetMediaByEntityAsync(EntityType.Review, reviewId);
		}
	}
}
