using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.GHNs;

namespace PerfumeGPT.Application.Validators.GHNs
{
    public class GhnOrderStatusWebhookRequestValidator : AbstractValidator<GhnOrderStatusWebhookRequest>
    {
        public GhnOrderStatusWebhookRequestValidator()
        {
            RuleFor(x => x.OrderCode)
                .NotEmpty().WithMessage("OrderCode là bắt buộc.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status là bắt buộc.");
        }
    }
}
