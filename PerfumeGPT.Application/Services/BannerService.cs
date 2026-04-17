using MapsterMapper;
using Microsoft.Extensions.Logging;
using PerfumeGPT.Application.DTOs.Requests.Banners;
using PerfumeGPT.Application.DTOs.Responses.Banners;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Extensions;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Application.Interfaces.Services;
using PerfumeGPT.Application.Interfaces.ThirdParties;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Application.Services
{
	public class BannerService : IBannerService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ISupabaseService _supabaseService;
		private readonly IBackgroundJobService _backgroundJobService;
		private readonly ILogger<BannerService> _logger;
		private const string BannerBucketName = "Banners";

		public BannerService(
			  IUnitOfWork unitOfWork,
			  IMapper mapper,
			  ISupabaseService supabaseService,
			  IBackgroundJobService backgroundJobService,
			  ILogger<BannerService> logger)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_supabaseService = supabaseService;
			_backgroundJobService = backgroundJobService;
			_logger = logger;
		}

		public async Task<BaseResponse<List<BannerResponse>>> GetVisibleBannersAsync(BannerPosition? position = null)
		{
			var banners = await _unitOfWork.Banners.GetVisibleBannersAsync(position);
			return BaseResponse<List<BannerResponse>>.Ok(banners, "Lấy danh sách banner hiển thị thành công.");
		}

		public async Task<BaseResponse<PagedResult<BannerResponse>>> GetPagedBannersAsync(GetPagedBannersRequest request)
		{
			var (items, totalCount) = await _unitOfWork.Banners.GetPagedBannersAsync(request);
			var pagedResult = new PagedResult<BannerResponse>(items, request.PageNumber, request.PageSize, totalCount);

			return BaseResponse<PagedResult<BannerResponse>>.Ok(pagedResult, "Lấy danh sách banner thành công.");
		}

		public async Task<BaseResponse<BannerResponse>> GetBannerByIdAsync(Guid bannerId)
		{
			var banner = await _unitOfWork.Banners.GetBannerByIdDtoAsync(bannerId)
				?? throw AppException.NotFound("Không tìm thấy banner.");

			return BaseResponse<BannerResponse>.Ok(banner, "Lấy thông tin banner thành công.");
		}

		public async Task<BaseResponse<string>> CreateBannerAsync(CreateBannerRequest request)
		{
			var temporaryDesktopImage = await GetValidTemporaryBannerMediaAsync(request.TemporaryImageId);
			TemporaryMedia? temporaryMobileImage = null;
			if (request.TemporaryMobileImageId.HasValue)
			{
				temporaryMobileImage = await GetValidTemporaryBannerMediaAsync(request.TemporaryMobileImageId.Value);
			}

			if (temporaryDesktopImage != null && temporaryMobileImage != null && temporaryDesktopImage.Id == temporaryMobileImage.Id)
			{
				throw AppException.BadRequest("Ảnh tạm cho desktop và mobile phải khác nhau.");
			}

			var creationPayload = _mapper.Map<Banner.BannerCreationPayload>(request) with
			{
				ImageUrl = temporaryDesktopImage!.Url,
				ImagePublicId = temporaryDesktopImage.PublicId,
				MobileImageUrl = temporaryMobileImage?.Url,
				MobileImagePublicId = temporaryMobileImage?.PublicId,
				AltText = request.AltText ?? temporaryDesktopImage?.AltText ?? temporaryMobileImage?.AltText
			};

			var banner = Banner.Create(creationPayload);

			banner.UpdateSchedule(request.StartDate, request.EndDate);
			banner.SetActiveStatus(request.IsActive);
			await HandleDisplayOrderAsync(banner, request.DisplayOrder);
			if (temporaryDesktopImage != null)
			{
				_unitOfWork.TemporaryMedia.Remove(temporaryDesktopImage);
			}

			if (temporaryMobileImage != null)
			{
				_unitOfWork.TemporaryMedia.Remove(temporaryMobileImage);
			}

			await _unitOfWork.SaveChangesAsync();

			ScheduleBannerJobs(banner);

			return BaseResponse<string>.Ok(banner.Id.ToString(), "Tạo banner thành công.");
		}

		public async Task<BaseResponse<string>> UpdateBannerAsync(Guid bannerId, UpdateBannerRequest request)
		{
			var banner = await _unitOfWork.Banners.GetByIdAsync(bannerId)
				?? throw AppException.NotFound("Không tìm thấy banner.");

			TemporaryMedia? temporaryDesktopImage = null;
			TemporaryMedia? temporaryMobileImage = null;

			if (request.TemporaryImageId.HasValue)
			{
				temporaryDesktopImage = await GetValidTemporaryBannerMediaAsync(request.TemporaryImageId.Value);
			}

			if (request.TemporaryMobileImageId.HasValue)
			{
				temporaryMobileImage = await GetValidTemporaryBannerMediaAsync(request.TemporaryMobileImageId.Value);
			}

			if (temporaryDesktopImage != null && temporaryMobileImage != null && temporaryDesktopImage.Id == temporaryMobileImage.Id)
			{
				throw AppException.BadRequest("Ảnh tạm cho desktop và mobile phải khác nhau.");
			}

			if (temporaryDesktopImage != null && !string.IsNullOrWhiteSpace(banner.ImagePublicId) && !string.IsNullOrWhiteSpace(banner.ImageUrl))
			{
				await _supabaseService.DeleteImageAsync(banner.ImageUrl, BannerBucketName);
			}

			if (temporaryMobileImage != null && !string.IsNullOrWhiteSpace(banner.MobileImagePublicId) && !string.IsNullOrWhiteSpace(banner.MobileImageUrl))
			{
				await _supabaseService.DeleteImageAsync(banner.MobileImageUrl, BannerBucketName);
			}

			banner.UpdateContent(
				request.Title,
				temporaryDesktopImage?.Url ?? banner.ImageUrl,
				temporaryDesktopImage?.PublicId ?? banner.ImagePublicId,
				temporaryMobileImage?.Url ?? banner.MobileImageUrl,
				temporaryMobileImage?.PublicId ?? banner.MobileImagePublicId,
				request.AltText ?? temporaryDesktopImage?.AltText ?? temporaryMobileImage?.AltText);

			banner.ChangePosition(request.Position);
			await HandleDisplayOrderAsync(banner, request.DisplayOrder);
			banner.UpdateSchedule(request.StartDate, request.EndDate);
			banner.UpdateLink(request.LinkType, request.LinkTarget);
			banner.SetActiveStatus(request.IsActive);

			if (temporaryDesktopImage != null)
			{
				_unitOfWork.TemporaryMedia.Remove(temporaryDesktopImage);
			}

			if (temporaryMobileImage != null)
			{
				_unitOfWork.TemporaryMedia.Remove(temporaryMobileImage);
			}

			await _unitOfWork.SaveChangesAsync();

			ScheduleBannerJobs(banner);

			return BaseResponse<string>.Ok(banner.Id.ToString(), "Cập nhật banner thành công.");
		}

		public async Task<BaseResponse<string>> DeleteBannerAsync(Guid bannerId)
		{
			var banner = await _unitOfWork.Banners.GetByIdAsync(bannerId)
				?? throw AppException.NotFound("Không tìm thấy banner.");

			_unitOfWork.Banners.Remove(banner);
			await _unitOfWork.SaveChangesAsync();

			return BaseResponse<string>.Ok(bannerId.ToString(), "Xóa banner thành công.");
		}

		private async Task<TemporaryMedia> GetValidTemporaryBannerMediaAsync(Guid temporaryMediaId)
		{
			var temporaryMedia = await _unitOfWork.TemporaryMedia.GetByIdAsync(temporaryMediaId)
			   ?? throw AppException.NotFound("Không tìm thấy media tạm.");

			if (temporaryMedia.TargetEntityType != EntityType.Banner)
			{
				throw AppException.BadRequest("Media tạm không dành cho banner.");
			}

			temporaryMedia.EnsureNotExpired();
			return temporaryMedia;
		}

		private void ScheduleBannerJobs(Banner banner)
		{
			if (banner.StartDate.HasValue)
			{
				_backgroundJobService.ScheduleBannerStart(_logger, banner.Id, banner.StartDate.Value);
			}

			if (banner.EndDate.HasValue)
			{
				_backgroundJobService.ScheduleBannerEnd(_logger, banner.Id, banner.EndDate.Value);
			}
		}

		private async Task HandleDisplayOrderAsync(Banner banner, int requestedOrder)
		{
			var existingBanners = (await _unitOfWork.Banners.GetAllAsync(
				   filter: b => b.Position == banner.Position && b.Id != banner.Id,
				   orderBy: q => q.OrderBy(b => b.DisplayOrder)))
				   .ToList();

			var maxOrder = existingBanners.Count == 0 ? 0 : existingBanners.Max(b => b.DisplayOrder);

			if (requestedOrder <= 0)
			{
				banner.ChangeOrder(maxOrder + 1);
				return;
			}

			var normalizedOrder = Math.Min(requestedOrder, maxOrder + 1);

			var isCollision = existingBanners.Any(b => b.DisplayOrder == normalizedOrder);

			if (isCollision)
			{
				var bannersToShift = existingBanners
				   .Where(b => b.DisplayOrder >= normalizedOrder)
					.OrderBy(b => b.DisplayOrder)
					.ToList();

				foreach (var b in bannersToShift)
				{
					b.ChangeOrder(b.DisplayOrder + 1);
					_unitOfWork.Banners.Update(b);
				}
			}

			banner.ChangeOrder(normalizedOrder);
		}
	}
}
