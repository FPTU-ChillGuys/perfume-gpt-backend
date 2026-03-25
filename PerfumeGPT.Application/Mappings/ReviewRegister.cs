using Mapster;
using PerfumeGPT.Application.DTOs.Responses.Reviews;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Mappings
{
	public class ReviewRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			config.NewConfig<Review, ReviewResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.UserId, src => src.UserId)
				.Map(dest => dest.UserFullName, src => src.User.FullName)
				.Map(dest => dest.UserProfilePictureUrl, src => src.User.ProfilePicture != null ? src.User.ProfilePicture.Url : null)
				.Map(dest => dest.OrderDetailId, src => src.OrderDetailId)
				.Map(dest => dest.VariantId, src => src.OrderDetail.VariantId)
				.Map(dest => dest.VariantName, src =>
					src.OrderDetail.ProductVariant.Product.Name + " " +
					src.OrderDetail.ProductVariant.VolumeMl + "ml " +
					src.OrderDetail.ProductVariant.Concentration.Name)
				.Map(dest => dest.Rating, src => src.Rating)
				.Map(dest => dest.Comment, src => src.Comment)
				.Map(dest => dest.StaffFeedbackComment, src => src.StaffFeedbackComment)
				.Map(dest => dest.StaffFeedbackAt, src => src.StaffFeedbackAt)
				.Map(dest => dest.Images, src => src.ReviewImages.Where(ri => !ri.IsDeleted))
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

			config.NewConfig<Review, ReviewDetailResponse>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.UserId, src => src.UserId)
				.Map(dest => dest.UserFullName, src => src.User.FullName)
				.Map(dest => dest.UserProfilePictureUrl, src => src.User.ProfilePicture != null ? src.User.ProfilePicture.Url : null)
				.Map(dest => dest.OrderDetailId, src => src.OrderDetailId)
				.Map(dest => dest.OrderId, src => src.OrderDetail.OrderId)
				.Map(dest => dest.Quantity, src => src.OrderDetail.Quantity)
				.Map(dest => dest.UnitPrice, src => src.OrderDetail.UnitPrice)
				.Map(dest => dest.VariantId, src => src.OrderDetail.VariantId)
				.Map(dest => dest.VariantName, src =>
					src.OrderDetail.ProductVariant.Product.Name + " " +
					src.OrderDetail.ProductVariant.VolumeMl + "ml " +
					src.OrderDetail.ProductVariant.Concentration.Name)
				.Map(dest => dest.ProductName, src => src.OrderDetail.ProductVariant.Product.Name)
				.Map(dest => dest.VolumeMl, src => src.OrderDetail.ProductVariant.VolumeMl)
				.Map(dest => dest.ConcentrationName, src => src.OrderDetail.ProductVariant.Concentration.Name)
				.Map(dest => dest.Rating, src => src.Rating)
				.Map(dest => dest.Comment, src => src.Comment)
				.Map(dest => dest.Images, src => src.ReviewImages.Where(ri => !ri.IsDeleted))
				.Map(dest => dest.StaffFeedbackComment, src => src.StaffFeedbackComment)
				.Map(dest => dest.StaffFeedbackByStaffId, src => src.StaffFeedbackByStaffId)
				.Map(dest => dest.StaffFeedbackAt, src => src.StaffFeedbackAt)
				.Map(dest => dest.CreatedAt, src => src.CreatedAt)
				.Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

			config.NewConfig<Review, ReviewListItem>()
				.Map(dest => dest.Id, src => src.Id)
				.Map(dest => dest.UserId, src => src.UserId)
				.Map(dest => dest.UserFullName, src => src.User.FullName)
				.Map(dest => dest.UserProfilePictureUrl, src => src.User.ProfilePicture != null ? src.User.ProfilePicture.Url : null)
				.Map(dest => dest.VariantId, src => src.OrderDetail.VariantId)
				.Map(dest => dest.VariantName, src =>
					src.OrderDetail.ProductVariant.Product.Name + " " +
					src.OrderDetail.ProductVariant.VolumeMl + "ml " +
					src.OrderDetail.ProductVariant.Concentration.Name)
				.Map(dest => dest.Rating, src => src.Rating)
				.Map(dest => dest.CommentPreview, src => src.Comment != null
					? src.Comment.Length > 100
						? $"{src.Comment.Substring(0, 100)}..."
						: src.Comment
					: string.Empty)
				.Map(dest => dest.ImageCount, src => src.ReviewImages.Count(ri => !ri.IsDeleted))
				.Map(dest => dest.CreatedAt, src => src.CreatedAt);
		}
	}
}
