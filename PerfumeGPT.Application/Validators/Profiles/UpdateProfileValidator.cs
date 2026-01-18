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
                .GreaterThanOrEqualTo(0).WithMessage("MaxBudget must be greater than or equal to 0.")
                .When(x => x.MaxBudget.HasValue);
            RuleFor(x => x.FavoriteNotes)
                .MaximumLength(500).WithMessage("FavoriteNotes cannot exceed 500 characters.")
                .When(x => !string.IsNullOrEmpty(x.FavoriteNotes));
            RuleFor(x => x.ScentPreference)
                .MaximumLength(200).WithMessage("ScentPreference cannot exceed 200 characters.")
                .When(x => !string.IsNullOrEmpty(x.ScentPreference));
            RuleFor(x => x.PreferredStyle)
                .MaximumLength(200).WithMessage("PreferredStyle cannot exceed 200 characters.")
                .When(x => !string.IsNullOrEmpty(x.PreferredStyle));
        }
    }
}
