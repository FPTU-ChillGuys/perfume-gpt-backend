using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Profiles;

namespace PerfumeGPT.Application.Validators.Profiles
{
	public class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
	{
		public UpdateProfileValidator()
		{
			RuleFor(x => x.MinBudget)
				.GreaterThanOrEqualTo(0).WithMessage("MinBudget must be greater than or equal to 0.")
				.When(x => x.MinBudget.HasValue);
			RuleFor(x => x.MaxBudget)
				.GreaterThanOrEqualTo(0).WithMessage("MaxBudget must be greater than or equal to 0.");

		}
	}
}
