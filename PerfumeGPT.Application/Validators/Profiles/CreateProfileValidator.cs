using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Profiles;

namespace PerfumeGPT.Application.Validators.Profiles
{
    public class CreateProfileValidator : AbstractValidator<CreateProfileRequest>
    {
        public CreateProfileValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.")
                .NotEqual(Guid.Empty).WithMessage("UserId cannot be an empty GUID.");
        }
    }
}
