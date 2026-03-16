using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Orders;

namespace PerfumeGPT.Application.Validators.RecipientInfos
{
	public class RecipientInformationValidator : AbstractValidator<RecipientInformation>
	{
		public RecipientInformationValidator()
		{
			RuleFor(r => r.RecipientName)
				.NotEmpty()
				.WithMessage("FullName is required");

			RuleFor(r => r.RecipientPhoneNumber)
				.NotEmpty()
				.WithMessage("Phone is required");

			RuleFor(r => r.DistrictId)
				.GreaterThan(0)
				.WithMessage("DistrictId is required");

			RuleFor(r => r.DistrictName)
				.NotEmpty()
				.WithMessage("DistrictName is required");

			RuleFor(r => r.WardCode)
				.NotEmpty()
				.WithMessage("WardCode is required");

			RuleFor(r => r.WardName)
				.NotEmpty()
				.WithMessage("WardName is required");

			RuleFor(r => r.ProvinceId)
				.GreaterThan(0)
				.WithMessage("ProvinceId is required");

			RuleFor(r => r.ProvinceName)
				.NotEmpty()
				.WithMessage("ProvinceName is required");

			RuleFor(r => r.FullAddress)
				.NotEmpty()
				.WithMessage("FullAddress is required");
		}
	}
}
