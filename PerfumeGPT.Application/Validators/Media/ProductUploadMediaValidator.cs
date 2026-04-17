using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Media;

namespace PerfumeGPT.Application.Validators.Media
{
	public class ProductUploadMediaValidator : AbstractValidator<ProductUploadMediaRequest>
	{
		public ProductUploadMediaValidator()
		{
			RuleFor(x => x.Images)
				.NotEmpty().WithMessage("Bắt buộc phải có ít nhất một hình ảnh.");

			RuleFor(x => x.Images).Custom((images, context) =>
			{
				if (images == null) return;

				// Ensure at most one primary image
				var primaryCount = images.Count(i => i.IsPrimary);
				if (primaryCount > 1)
				{
					context.AddFailure("Chỉ được phép đánh dấu một hình ảnh là chính.");
				}

				// Ensure no duplicate display orders
				var duplicateOrders = images.GroupBy(i => i.DisplayOrder)
					.Where(g => g.Count() > 1)
					.Select(g => g.Key)
					.ToList();

				if (duplicateOrders.Count != 0)
				{
					context.AddFailure($"Các giá trị thứ tự hiển thị trùng lặp được tìm thấy: {string.Join(", ", duplicateOrders)}");
				}
			});

			// Validate each image item
			RuleForEach(x => x.Images).ChildRules(item =>
			{
				item.RuleFor(i => i.ImageFile).NotNull().WithMessage("Tệp hình ảnh là bắt buộc.");
				item.RuleFor(i => i.DisplayOrder).GreaterThanOrEqualTo(0).WithMessage("Thứ tự hiển thị phải >= 0.");
			});
		}
	}
}
