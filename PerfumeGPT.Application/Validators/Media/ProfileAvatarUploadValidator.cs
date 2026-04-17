using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Media;

namespace PerfumeGPT.Application.Validators.Media
{
	public class ProfileAvatarUploadValidator : AbstractValidator<ProfileAvtarUploadRequest>
	{
		public ProfileAvatarUploadValidator()
		{
			RuleFor(x => x.Avatar)
				.NotNull().WithMessage("Ảnh đại diện là bắt buộc.")
				.Must(file => file != null && file.Length > 0).WithMessage("Ảnh đại diện không được để trống.")
				.Must(file => file.Length <= 5 * 1024 * 1024).WithMessage("Kích thước ảnh đại diện phải nhỏ hơn hoặc bằng 5MB.");
		}
	}
}
