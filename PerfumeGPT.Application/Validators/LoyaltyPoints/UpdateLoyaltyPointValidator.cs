using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.LoyaltyPoints;

namespace PerfumeGPT.Application.Validators.LoyaltyPoints
{
    public class UpdateLoyaltyPointValidator : AbstractValidator<UpdateLoyaltyPointRequest>
    {
        public UpdateLoyaltyPointValidator()
        {
            RuleFor(x => x.PointBalance)
                .GreaterThanOrEqualTo(0).WithMessage("PointBalance must be greater than or equal to 0.");
        }
    }
}
