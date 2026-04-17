using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Users;

namespace PerfumeGPT.Application.Validators.Users
{
	public class UpdateUserBasicInfoValidator : AbstractValidator<UpdateUserBasicInfoRequest>
	{
		public UpdateUserBasicInfoValidator()
		{
			RuleFor(x => x.FullName)
			   .NotEmpty().WithMessage("Họ và tên là bắt buộc.")
				.MaximumLength(100).WithMessage("Họ và tên không được vượt quá 100 ký tự.");

			RuleFor(x => x.PhoneNumber)
				.NotEmpty().WithMessage("Số điện thoại là bắt buộc.")
				.Matches("^[0-9+]{8,15}$").WithMessage("Định dạng số điện thoại không hợp lệ.");
		}
	}
}
