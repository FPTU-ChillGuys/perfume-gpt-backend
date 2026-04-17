using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Reviews;

namespace PerfumeGPT.Application.Validators.Reviews
{
	public class AnswerReviewValidator : AbstractValidator<AnswerReviewRequest>
	{
		private const int MaxCommentLength = 2000;
		private const int MinCommentLength = 2;

		public AnswerReviewValidator()
		{
			RuleFor(x => x.StaffFeedbackComment)
				.NotEmpty()
             .WithMessage("Nội dung phản hồi của nhân viên là bắt buộc.")
				.MinimumLength(MinCommentLength)
             .WithMessage($"Nội dung phản hồi của nhân viên phải có ít nhất {MinCommentLength} ký tự.")
				.MaximumLength(MaxCommentLength)
             .WithMessage($"Nội dung phản hồi của nhân viên không được vượt quá {MaxCommentLength} ký tự.");
		}
	}
}
