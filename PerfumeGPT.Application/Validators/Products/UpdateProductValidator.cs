using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Products;

namespace PerfumeGPT.Application.Validators.Products
{
	public class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
	{
		public UpdateProductValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Product name is required.")
				.MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");
			RuleFor(x => x.BrandId)
				.GreaterThan(0).WithMessage("BrandId must be a positive integer.");
			RuleFor(x => x.CategoryId)
				.GreaterThan(0).WithMessage("CategoryId must be a positive integer.");
			RuleFor(x => x.FamilyId)
				.GreaterThan(0).WithMessage("FamilyId must be a positive integer.");
			RuleFor(x => x.Description)
				.MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");
			RuleFor(x => x.TopNotes)
				.MaximumLength(500).WithMessage("Top notes must not exceed 500 characters.");
			RuleFor(x => x.MiddleNotes)
				.MaximumLength(500).WithMessage("Middle notes must not exceed 500 characters.");
			RuleFor(x => x.BaseNotes)
				.MaximumLength(500).WithMessage("Base notes must not exceed 500 characters.");
		}
	}
}
