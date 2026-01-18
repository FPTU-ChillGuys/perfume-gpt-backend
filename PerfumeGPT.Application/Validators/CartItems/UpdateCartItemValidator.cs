using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.CartItems;

namespace PerfumeGPT.Application.Validators.CartItems
{
    public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemRequest>
    {
        public UpdateCartItemValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        }
    }
}
