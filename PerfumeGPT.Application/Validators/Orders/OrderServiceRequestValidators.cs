using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.Orders.OrderDetails;
using PerfumeGPT.Application.Validators.RecipientInfos;

namespace PerfumeGPT.Application.Validators.Orders
{
	public class UpdateOrderAddressRequestValidator : AbstractValidator<UpdateOrderAddressRequest>
	{
		public UpdateOrderAddressRequestValidator()
		{
			RuleFor(x => x)
				.Must(x => x.SavedAddressId.HasValue || x.RecipientInformation != null)
				.WithMessage("Either saved address ID or recipient information must be provided.");

			When(x => x.RecipientInformation != null, () =>
			{
				RuleFor(x => x.RecipientInformation!)
					.SetValidator(new RecipientInformationValidator());
			});
		}
	}

	public class GetPagedOrdersRequestValidator : AbstractValidator<GetPagedOrdersRequest>
	{
		public GetPagedOrdersRequestValidator()
		{
			RuleFor(x => x.PageNumber)
				.GreaterThan(0).WithMessage("Page number must be greater than 0.");

			RuleFor(x => x.PageSize)
				.GreaterThan(0).WithMessage("Page size must be greater than 0.")
				.LessThanOrEqualTo(50).WithMessage("Page size must be less than or equal to 50.");

			RuleFor(x => x.SortOrder)
				.Must(order => string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase))
				.WithMessage("Sort order must be either 'asc' or 'desc'.");

			RuleFor(x => x)
				.Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
				.WithMessage("From date must be less than or equal to to date.");

			RuleFor(x => x.SearchTerm)
				.MaximumLength(200).WithMessage("Search term must not exceed 200 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));
		}
	}

	public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
	{
		public CreateOrderRequestValidator()
		{
			RuleFor(x => x.ItemIds)
				.Must(ids => ids.Distinct().Count() == ids.Count)
                .WithMessage("Duplicate item IDs are not allowed.")
				.When(x => x.ItemIds != null);

			RuleFor(x => x.ExpectedTotalPrice)
				.GreaterThanOrEqualTo(0).WithMessage("Expected total price must be greater than or equal to 0.")
				.When(x => x.ExpectedTotalPrice.HasValue);

			RuleFor(x => x.DeliveryMethod)
				.IsInEnum().WithMessage("Invalid delivery method.");

			RuleFor(x => x.Payment)
				.NotNull().WithMessage("Payment information is required.");

			RuleFor(x => x.Payment.Method)
				.IsInEnum().WithMessage("Invalid payment method.");

			RuleFor(x => x)
				.Must(x => x.DeliveryMethod != Domain.Enums.DeliveryMethod.Delivery || x.SavedAddressId.HasValue || x.Recipient != null)
				.WithMessage("Delivery orders require a saved address ID or recipient information.");

			RuleFor(x => x.VoucherCode)
				.MaximumLength(50).WithMessage("Voucher code must not exceed 50 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.VoucherCode));

			When(x => x.Recipient != null, () =>
			{
				RuleFor(x => x.Recipient!)
					.SetValidator(new RecipientInformationValidator());
			});
		}
	}

	public class CreateInStoreOrderRequestValidator : AbstractValidator<CreateInStoreOrderRequest>
	{
		public CreateInStoreOrderRequestValidator()
		{
			RuleFor(x => x.OrderDetails)
				.NotEmpty().WithMessage("At least one order detail is required.");

			RuleForEach(x => x.OrderDetails)
				.SetValidator(new CreateOrderDetailRequestValidator());

			RuleFor(x => x.Payment)
				.NotNull().WithMessage("Payment information is required.");

			RuleFor(x => x.Payment.Method)
				.IsInEnum().WithMessage("Invalid payment method.");

			RuleFor(x => x)
				.Must(x => x.IsPickupInStore || x.Recipient != null)
				.WithMessage("Recipient information is required when not picking up in store.");

			RuleFor(x => x.VoucherCode)
				.MaximumLength(50).WithMessage("Voucher code must not exceed 50 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.VoucherCode));

			When(x => x.Recipient != null, () =>
			{
				RuleFor(x => x.Recipient!)
					.SetValidator(new RecipientInformationValidator());
			});
		}
	}

	public class CreateOrderDetailRequestValidator : AbstractValidator<CreateOrderDetailRequest>
	{
		public CreateOrderDetailRequestValidator()
		{
			RuleFor(x => x.VariantId)
				.NotEmpty().WithMessage("Variant ID is required.");

			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Quantity must be greater than 0.");
		}
	}

	public class PreviewOrderRequestValidator : AbstractValidator<PreviewOrderRequest>
	{
		public PreviewOrderRequestValidator()
		{
			RuleFor(x => x.BarCodes)
				.NotEmpty().WithMessage("At least one barcode is required.");

			RuleForEach(x => x.BarCodes)
				.NotEmpty().WithMessage("Barcode cannot be empty.");

			RuleFor(x => x.VoucherCode)
				.MaximumLength(50).WithMessage("Voucher code must not exceed 50 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.VoucherCode));
		}
	}

	public class UserCancelOrderRequestValidator : AbstractValidator<UserCancelOrderRequest>
	{
		public UserCancelOrderRequestValidator()
		{
			RuleFor(x => x.Reason)
				.MaximumLength(1000).WithMessage("Reason must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.Reason));
		}
	}

	public class FulfillOrderRequestValidator : AbstractValidator<FulfillOrderRequest>
	{
		public FulfillOrderRequestValidator()
		{
			RuleFor(x => x.Items)
				.NotEmpty().WithMessage("Fulfillment items are required.");

			RuleFor(x => x.Items)
				.Must(items => items.Select(i => i.OrderDetailId).Distinct().Count() == items.Count)
				.WithMessage("Duplicate order detail IDs in fulfillment items are not allowed.");

			RuleForEach(x => x.Items)
				.SetValidator(new FulfillOrderItemRequestValidator());
		}
	}

	public class FulfillOrderItemRequestValidator : AbstractValidator<FulfillOrderItemRequest>
	{
		public FulfillOrderItemRequestValidator()
		{
			RuleFor(x => x.OrderDetailId)
				.NotEmpty().WithMessage("Order detail ID is required.");

			RuleFor(x => x.ScannedBatchCode)
				.NotEmpty().WithMessage("Scanned batch code is required.");

			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Quantity must be greater than 0.");
		}
	}

	public class SwapDamagedStockRequestValidator : AbstractValidator<SwapDamagedStockRequest>
	{
		public SwapDamagedStockRequestValidator()
		{
			RuleFor(x => x.DamagedReservationId)
				.NotEmpty().WithMessage("Damaged reservation ID is required.");

			RuleFor(x => x.DamageNote)
				.MaximumLength(1000).WithMessage("Damage note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.DamageNote));
		}
	}
}
