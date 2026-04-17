using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Reviews;

namespace PerfumeGPT.Application.Validators.Reviews
{
	public class CreateReviewValidator : AbstractValidator<CreateReviewRequest>
	{
		private const int MaxCommentLength = 2000;
		private const int MinCommentLength = 10;

		public CreateReviewValidator()
		{
			RuleFor(x => x.OrderDetailId)
				.NotEmpty()
               .WithMessage("Order detail ID là bắt buộc.");

			RuleFor(x => x.Rating)
				.InclusiveBetween(1, 5)
              .WithMessage("Đánh giá phải nằm trong khoảng từ 1 đến 5 sao.");

			RuleFor(x => x.Comment)
				.NotEmpty()
                .WithMessage("Nội dung đánh giá là bắt buộc.")
				.MinimumLength(MinCommentLength)
                .WithMessage($"Nội dung đánh giá phải có ít nhất {MinCommentLength} ký tự.")
				.MaximumLength(MaxCommentLength)
                .WithMessage($"Nội dung đánh giá không được vượt quá {MaxCommentLength} ký tự.");
		}
	}
}
