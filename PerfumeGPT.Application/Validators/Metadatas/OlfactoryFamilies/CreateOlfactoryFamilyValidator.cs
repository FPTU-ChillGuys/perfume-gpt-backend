using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies;

namespace PerfumeGPT.Application.Validators.Metadatas.OlfactoryFamilies
{
	public class CreateOlfactoryFamilyValidator : AbstractValidator<CreateOlfactoryFamilyRequest>
	{
		public CreateOlfactoryFamilyValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Olfactory family name is required.")
				.MaximumLength(100).WithMessage("Olfactory family name cannot exceed 100 characters.");
		}
	}
}
