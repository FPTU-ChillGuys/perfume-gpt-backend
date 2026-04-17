using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Banners;

namespace PerfumeGPT.Application.Validators.Banners
{
	public class UpdateBannerValidator : AbstractValidator<UpdateBannerRequest>
	{
		public UpdateBannerValidator()
		{
			RuleFor(x => x.Title)
				.Must(title => !string.IsNullOrWhiteSpace(title))
				.WithMessage("Tiêu đề là bắt buộc.")
				.MaximumLength(200)
				.WithMessage("Tiêu đề không được vượt quá 200 ký tự.");

			RuleFor(x => x.TemporaryImageId)
				.Must(id => !id.HasValue || id.Value != Guid.Empty)
				.WithMessage("TemporaryImageId phải là một guid hợp lệ.");
			RuleFor(x => x.TemporaryMobileImageId)
				.Must(id => !id.HasValue || id.Value != Guid.Empty)
				.WithMessage("TemporaryMobileImageId phải là một guid hợp lệ.");

			RuleFor(x => x)
				.Must(x => !x.TemporaryImageId.HasValue || !x.TemporaryMobileImageId.HasValue || x.TemporaryImageId.Value != x.TemporaryMobileImageId.Value)
				.WithMessage("Hình ảnh tạm thời cho desktop và mobile phải khác nhau.");
			RuleFor(x => x.LinkTarget)
				.Must(target => !string.IsNullOrWhiteSpace(target))
				.WithMessage("Link target là bắt buộc.");

			RuleFor(x => x.DisplayOrder)
				.GreaterThanOrEqualTo(0)
				.WithMessage("Display order phải lớn hơn hoặc bằng 0.");
			RuleFor(x => x.Position)
				.IsInEnum().WithMessage("Vị trí banner không hợp lệ.");

			RuleFor(x => x.LinkType)
				.IsInEnum().WithMessage("Loại liên kết banner không hợp lệ.");

			RuleFor(x => x)
				.Must(x => !x.StartDate.HasValue || !x.EndDate.HasValue || x.StartDate < x.EndDate)
				.WithMessage("Ngày kết thúc phải sau ngày bắt đầu.");
		}
	}
}
