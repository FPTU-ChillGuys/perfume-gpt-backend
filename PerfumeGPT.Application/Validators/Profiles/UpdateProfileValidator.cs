using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Profiles;

namespace PerfumeGPT.Application.Validators.Profiles
{
	public class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
	{
		public UpdateProfileValidator()
		{
			RuleFor(x => x.MinBudget)
               .GreaterThanOrEqualTo(0).WithMessage("Ngân sách tối thiểu phải lớn hơn hoặc bằng 0.")
				.When(x => x.MinBudget.HasValue);
			RuleFor(x => x.MaxBudget)
              .GreaterThanOrEqualTo(0).WithMessage("Ngân sách tối đa phải lớn hơn hoặc bằng 0.");
			RuleFor(x => x.DateOfBirth)
				.LessThan(DateTime.UtcNow.AddYears(16))
               .When(x => x.DateOfBirth.HasValue).WithMessage("Bạn phải từ 16 tuổi trở lên.");
		}
	}
}
