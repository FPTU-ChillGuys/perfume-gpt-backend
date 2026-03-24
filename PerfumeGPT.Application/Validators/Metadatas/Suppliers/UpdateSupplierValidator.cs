using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Metadatas.Suppliers;

namespace PerfumeGPT.Application.Validators.Metadatas.Suppliers
{
	public class UpdateSupplierValidator : AbstractValidator<UpdateSupplierRequest>
	{
		public UpdateSupplierValidator()
		{
			RuleFor(x => x.Name)
				.NotEmpty().WithMessage("Supplier name is required.")
				.MaximumLength(150).WithMessage("Supplier name must not exceed 150 characters.");

			RuleFor(x => x.ContactEmail)
				.NotEmpty().WithMessage("Supplier contact email is required.")
				.EmailAddress().WithMessage("Supplier contact email is invalid.")
				.MaximumLength(254).WithMessage("Supplier contact email must not exceed 254 characters.");

			RuleFor(x => x.Phone)
				.NotEmpty().WithMessage("Supplier phone is required.")
				.Matches(@"^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$").WithMessage("Invalid phone number format.");

			RuleFor(x => x.Address)
				.NotEmpty().WithMessage("Supplier address is required.")
				.MaximumLength(255).WithMessage("Supplier address must not exceed 255 characters.");
		}
	}
}
