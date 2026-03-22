using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Address;

namespace PerfumeGPT.Application.Validators.Address
{
	public class CreateAddressValidator : AbstractValidator<CreateAddressRequest>
	{
		public CreateAddressValidator()
		{
			RuleFor(x => x.RecipientName)
				.NotEmpty().WithMessage("Recipient name is required.")
				.MaximumLength(100).WithMessage("Recipient name must not exceed 100 characters.");

			RuleFor(x => x.RecipientPhoneNumber)
				.NotEmpty().WithMessage("Recipient phone number is required.")
				.Matches(@"^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$").WithMessage("Invalid phone number format.");

			RuleFor(x => x.Street)
				.MaximumLength(200).WithMessage("Street must not exceed 200 characters.");

			RuleFor(x => x.Ward)
				.MaximumLength(100).WithMessage("Ward must not exceed 100 characters.");

			RuleFor(x => x.District)
				.MaximumLength(100).WithMessage("District must not exceed 100 characters.");

			RuleFor(x => x.City)
				.MaximumLength(100).WithMessage("City must not exceed 100 characters.");

			RuleFor(x => x.WardCode)
				.NotEmpty().WithMessage("Ward code is required.");

			RuleFor(x => x.DistrictId)
				.GreaterThan(0).WithMessage("District ID must be a positive integer.");

			RuleFor(x => x.ProvinceId)
				.GreaterThan(0).WithMessage("Province ID must be a positive integer.");
		}
	}
}
