using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Media;

namespace PerfumeGPT.Application.Validators.Media
{
	public class ProfileAvatarUploadValidator : AbstractValidator<ProfileAvtarUploadRequest>
	{
		public ProfileAvatarUploadValidator()
		{
			RuleFor(x => x.Avatar)
				.NotNull().WithMessage("Avatar file is required.")
				.Must(file => file != null && file.Length > 0).WithMessage("Avatar file cannot be empty.")
				.Must(file => file.Length <= 5 * 1024 * 1024).WithMessage("Avatar file size must be less than or equal to 5MB.");
		}
	}
}
