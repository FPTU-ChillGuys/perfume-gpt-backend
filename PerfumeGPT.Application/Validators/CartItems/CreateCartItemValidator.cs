using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.CartItems;

namespace PerfumeGPT.Application.Validators.CartItems
{
    public class CreateCartItemValidator : AbstractValidator<CreateCartItemRequest>
    {
        public CreateCartItemValidator()
        {
            RuleFor(x => x.CartId)
                .NotEmpty().WithMessage("CartId is required.")
                .Must(cartId => cartId != Guid.Empty).WithMessage("CartId cannot be an empty GUID.");
            RuleFor(x => x.VariantId)
                .NotEmpty().WithMessage("VariantId is required.")
                .Must(variantId => variantId != Guid.Empty).WithMessage("VariantId cannot be an empty GUID.");
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than 0.");
        }
    }
}
