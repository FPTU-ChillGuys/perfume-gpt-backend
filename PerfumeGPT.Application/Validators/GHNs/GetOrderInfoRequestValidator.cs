using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.GHNs;

namespace PerfumeGPT.Application.Validators.GHNs
{
	public class GetOrderInfoRequestValidator : AbstractValidator<GetOrderInfoRequest>
	{
		public GetOrderInfoRequestValidator()
		{
			RuleFor(x => x.TrackingNumbers)
				.NotNull().WithMessage("Tracking numbers are required.")
				.Must(x => x.Count > 0).WithMessage("Tracking numbers must contain at least one value.");

			RuleForEach(x => x.TrackingNumbers)
				.NotEmpty().WithMessage("Tracking number cannot be empty.");
		}
	}
}
