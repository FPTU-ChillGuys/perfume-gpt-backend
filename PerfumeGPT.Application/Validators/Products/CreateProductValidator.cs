using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Products;

namespace PerfumeGPT.Application.Validators.Products
{
	public class CreateProductValidator : AbstractValidator<CreateProductRequest>
	{
		public CreateProductValidator()
		{
			var maxAllowedReleaseYear = DateTime.UtcNow.Year + 1;

			RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Tên sản phẩm là bắt buộc.")
				.MaximumLength(200).WithMessage("Tên sản phẩm không được vượt quá 200 ký tự.");

			RuleFor(x => x.BrandId)
             .GreaterThan(0).WithMessage("BrandId phải là số nguyên dương.");

			RuleFor(x => x.CategoryId)
              .GreaterThan(0).WithMessage("CategoryId phải là số nguyên dương.");

			RuleFor(x => x.Origin)
              .NotEmpty().WithMessage("Xuất xứ là bắt buộc.")
				.MaximumLength(100).WithMessage("Xuất xứ không được vượt quá 100 ký tự.");

			RuleFor(x => x.Gender)
              .IsInEnum().WithMessage("Giới tính không hợp lệ.");

			RuleFor(x => x.ReleaseYear)
			  .InclusiveBetween(1900, maxAllowedReleaseYear)
              .WithMessage("Năm phát hành không hợp lệ.");

			RuleForEach(x => x.OlfactoryFamilyIds)
               .GreaterThan(0).WithMessage("OlfactoryFamilyIds phải là số nguyên dương.");

			RuleForEach(x => x.ScentNotes)
				.ChildRules(note =>
				{
					note.RuleFor(n => n.NoteId)
                       .GreaterThan(0).WithMessage("ID nốt hương phải là số nguyên dương.");
					note.RuleFor(n => n.Type)
                     .IsInEnum().WithMessage("Loại nốt hương không hợp lệ.");
				});
		}
	}
}
