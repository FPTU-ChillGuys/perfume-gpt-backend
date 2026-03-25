using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Concentrations;

namespace PerfumeGPT.Application.Validators.Metadatas.Concentrations
{
	public class CreateConcentrationValidator : AbstractValidator<CreateConcentrationRequest>
	{
		public CreateConcentrationValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Name is required.")
				.MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
		}
	}
}
