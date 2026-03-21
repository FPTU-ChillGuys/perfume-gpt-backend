using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Campaigns;

namespace PerfumeGPT.Application.Validators.Campaigns
{
	public class UpdateCampaignValidator : AbstractValidator<UpdateCampaignRequest>
	{
		public UpdateCampaignValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Campaign name is required.")
				.MaximumLength(100).WithMessage("Campaign name must not exceed 100 characters.");

			RuleFor(x => x.EndDate)
				.GreaterThan(x => x.StartDate).WithMessage("End date must be after start date.");
		}
	}
}
