using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Address;

namespace PerfumeGPT.Application.Validators.Address
{
	public class CreateAddressValidator : AbstractValidator<CreateAddressRequest>
	{
		public CreateAddressValidator()
		{
			RuleFor(x => x.RecipientName)
				.NotEmpty().WithMessage("Tên người nhận là bắt buộc.")
				.MaximumLength(100).WithMessage("Tên người nhận phải không vượt quá 100 ký tự.");

			RuleFor(x => x.RecipientPhoneNumber)
				.NotEmpty().WithMessage("Số điện thoại người nhận là bắt buộc.")
				.Matches(@"^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$").WithMessage("Định dạng số điện thoại không hợp lệ.");

			RuleFor(x => x.Street)
				.MaximumLength(200).WithMessage("Địa chỉ không được vượt quá 200 ký tự.");
			RuleFor(x => x.Ward)
				.MaximumLength(100).WithMessage("Phường/Xã không được vượt quá 100 ký tự.");

			RuleFor(x => x.District)
				.MaximumLength(100).WithMessage("Quận/Huyện không được vượt quá 100 ký tự.");

			RuleFor(x => x.City)
				.MaximumLength(100).WithMessage("Thành phố không được vượt quá 100 ký tự.");

			RuleFor(x => x.WardCode)
				.NotEmpty().WithMessage("Mã phường/Xã là bắt buộc.");

			RuleFor(x => x.DistrictId)
				.GreaterThan(0).WithMessage("Mã quận/huyện phải là số nguyên dương.");

			RuleFor(x => x.ProvinceId)
				.GreaterThan(0).WithMessage("Mã tỉnh/thành phố phải là số nguyên dương.");
		}
	}
}
