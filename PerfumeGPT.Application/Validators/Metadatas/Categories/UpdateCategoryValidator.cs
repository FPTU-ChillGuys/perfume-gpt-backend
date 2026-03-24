using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Categories;

namespace PerfumeGPT.Application.Validators.Metadatas.Categories
{
	public class UpdateCategoryValidator : AbstractValidator<UpdateCategoryRequest>
	{
		public UpdateCategoryValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Category name is required.")
				.MaximumLength(100).WithMessage("Category name must not exceed 100 characters.");
		}
	}
}
