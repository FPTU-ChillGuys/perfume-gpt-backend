using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Carts;

namespace PerfumeGPT.Application.Validators.Carts
{
    public class CreateCartValidator : AbstractValidator<CreateCartRequest>
    {
        public CreateCartValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("UserId is required.")
                .NotEqual(Guid.Empty).WithMessage("UserId cannot be an empty GUID.");
        }
    }
}
