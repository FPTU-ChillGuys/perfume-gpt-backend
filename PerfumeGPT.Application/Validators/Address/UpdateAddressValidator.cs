using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Address;

namespace PerfumeGPT.Application.Validators.Address
{
	public class UpdateAddressValidator : AbstractValidator<UpdateAddressRequest>
	{
		public UpdateAddressValidator()
		{
			RuleFor(x => x.ReceiverName)
				.NotEmpty().WithMessage("Receiver name is required.")
				.MaximumLength(100).WithMessage("Receiver name must not exceed 100 characters.");
			RuleFor(x => x.Phone)
				.NotEmpty().WithMessage("Phone number is required.")
				.MaximumLength(15).WithMessage("Phone number must not exceed 15 characters.");
			RuleFor(x => x.Street)
				.MaximumLength(200).WithMessage("Street must not exceed 200 characters.");
			RuleFor(x => x.WardCode)
				.NotEmpty().WithMessage("Ward code is required.");
			RuleFor(x => x.DistrictId)
				.GreaterThan(0).WithMessage("District ID must be a positive integer.");
			RuleFor(x => x.ProvinceId)
				.GreaterThan(0).WithMessage("Province ID must be a positive integer.");
		}
	}
}
