using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Orders;

namespace PerfumeGPT.Application.Validators.ContactAddresses
{
	public class ContactAddressInformationValidator : AbstractValidator<ContactAddressInformation>
	{
		public ContactAddressInformationValidator()
		{
			RuleFor(r => r.ContactName)
				.NotEmpty()
				.WithMessage("Tên liên hệ là bắt buộc");

			RuleFor(r => r.ContactPhoneNumber)
				.NotEmpty()
				.WithMessage("Số điện thoại liên hệ là bắt buộc");

			RuleFor(r => r.DistrictId)
				.GreaterThan(0)
				.WithMessage("DistrictId là bắt buộc");
			RuleFor(r => r.DistrictName)
				.NotEmpty()
				.WithMessage("Tên quận/huyện là bắt buộc");

			RuleFor(r => r.WardCode)
				.NotEmpty()
				.WithMessage("Mã phường/xã là bắt buộc");
			RuleFor(r => r.WardName)
				.NotEmpty()
				.WithMessage("Tên phường/xã là bắt buộc");

			RuleFor(r => r.ProvinceId)
				.GreaterThan(0)
				.WithMessage("ProvinceId là bắt buộc");
			RuleFor(r => r.ProvinceName)
				.NotEmpty()
				.WithMessage("Tên tỉnh/thành phố là bắt buộc");

			RuleFor(r => r.FullAddress)
				.NotEmpty()
				.WithMessage("Địa chỉ đầy đủ là bắt buộc");
		}
	}
}
