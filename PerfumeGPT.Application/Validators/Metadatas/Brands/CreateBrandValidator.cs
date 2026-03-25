using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Brands;

namespace PerfumeGPT.Application.Validators.Metadatas.Brands
{
	public class CreateBrandValidator : AbstractValidator<CreateBrandRequest>
	{
		public CreateBrandValidator()
		{
			RuleFor(x => x.Name)
				.Must(name => !string.IsNullOrWhiteSpace(name))
				.WithMessage("Brand name is required.")
				.MaximumLength(100)
				.WithMessage("Brand name must not exceed 100 characters.");
		}
	}
}
