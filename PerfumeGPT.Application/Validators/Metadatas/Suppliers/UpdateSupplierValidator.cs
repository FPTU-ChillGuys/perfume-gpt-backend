using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Suppliers;

namespace PerfumeGPT.Application.Validators.Metadatas.Suppliers
{
	public class UpdateSupplierValidator : AbstractValidator<UpdateSupplierRequest>
	{
		public UpdateSupplierValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Tên nhà cung cấp là bắt buộc.")
				.MaximumLength(150).WithMessage("Tên nhà cung cấp không được vượt quá 150 ký tự.");

			RuleFor(x => x.ContactEmail)
				.NotEmpty().WithMessage("Email liên hệ của nhà cung cấp là bắt buộc.")
				.EmailAddress().WithMessage("Email liên hệ của nhà cung cấp không hợp lệ.")
				.MaximumLength(254).WithMessage("Email liên hệ của nhà cung cấp không được vượt quá 254 ký tự.");

			RuleFor(x => x.Phone)
				.NotEmpty().WithMessage("Số điện thoại của nhà cung cấp là bắt buộc.")
				.Matches(@"^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$").WithMessage("Định dạng số điện thoại không hợp lệ.");
			RuleFor(x => x.Address)
				.NotEmpty().WithMessage("Địa chỉ của nhà cung cấp là bắt buộc.")
				.MaximumLength(255).WithMessage("Địa chỉ của nhà cung cấp không được vượt quá 255 ký tự.");
		}
	}
}
