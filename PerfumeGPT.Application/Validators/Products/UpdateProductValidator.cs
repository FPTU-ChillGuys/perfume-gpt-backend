using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Products;

namespace PerfumeGPT.Application.Validators.Products
{
	public class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
	{
		public UpdateProductValidator()
		{
            var maxAllowedReleaseYear = DateTime.UtcNow.Year + 1;

			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Product name cannot be empty.")
				.MaximumLength(200).WithMessage("Product name must not exceed 200 characters.")
				.When(x => x.Name is not null);

			RuleFor(x => x.BrandId)
				.GreaterThan(0).WithMessage("BrandId must be a positive integer.");

			RuleFor(x => x.CategoryId)
				.GreaterThan(0).WithMessage("CategoryId must be a positive integer.");

			RuleFor(x => x.Origin)
				.NotEmpty().WithMessage("Origin cannot be empty.")
				.MaximumLength(100).WithMessage("Origin must not exceed 100 characters.");

			RuleFor(x => x.Gender)
				.IsInEnum().WithMessage("Gender is invalid.");

			RuleFor(x => x.ReleaseYear)
				.InclusiveBetween(1900, maxAllowedReleaseYear)
				.WithMessage("Invalid release year.");

			RuleForEach(x => x.OlfactoryFamilyIds)
				.GreaterThan(0).WithMessage("OlfactoryFamilyIds must be positive integers.");

			RuleForEach(x => x.ScentNotes)
				.ChildRules(note =>
				{
					note.RuleFor(n => n.NoteId)
						.GreaterThan(0).WithMessage("Scent note id must be a positive integer.");
					note.RuleFor(n => n.Type)
						.IsInEnum().WithMessage("Scent note type is invalid.");
				});
		}
	}
}
