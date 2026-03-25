using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.OlfactoryFamilies;

namespace PerfumeGPT.Application.Validators.Metadatas.OlfactoryFamilies
{
	public class UpdateOlfactoryFamilyValidator : AbstractValidator<UpdateOlfactoryFamilyRequest>
	{
		public UpdateOlfactoryFamilyValidator()
		{
			RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.")
				.MaximumLength(100).WithMessage("Name must not exceed 100 characters.");
		}
	}
}
