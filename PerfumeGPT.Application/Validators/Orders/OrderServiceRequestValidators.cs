using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Application.DTOs.Requests.Orders.OrderDetails;
using PerfumeGPT.Application.Validators.ContactAddresses;

namespace PerfumeGPT.Application.Validators.Orders
{
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
					.SetValidator(new ContactAddressInformationValidator());
			});
		}
	}

	public class CreateInStoreOrderRequestValidator : AbstractValidator<CreateInStoreOrderRequest>
	{
		public CreateInStoreOrderRequestValidator()
		{
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
					.SetValidator(new ContactAddressInformationValidator());
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
			RuleFor(x => x.Reason).IsInEnum().WithMessage("Invalid cancellation reason.");

			RuleFor(x => x.RefundBankName)
				.MaximumLength(255).WithMessage("Refund bank name must not exceed 255 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundBankName));

			RuleFor(x => x.RefundAccountNumber)
				.MaximumLength(50).WithMessage("Refund account number must not exceed 50 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundAccountNumber));

			RuleFor(x => x.RefundAccountName)
				.MaximumLength(255).WithMessage("Refund account name must not exceed 255 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundAccountName));

			RuleFor(x => x)
				.Must(x =>
				{
					var hasBankInfo = !string.IsNullOrWhiteSpace(x.RefundBankName)
						|| !string.IsNullOrWhiteSpace(x.RefundAccountNumber)
						|| !string.IsNullOrWhiteSpace(x.RefundAccountName);

					if (!hasBankInfo)
						return true;

					return !string.IsNullOrWhiteSpace(x.RefundBankName)
						&& !string.IsNullOrWhiteSpace(x.RefundAccountNumber)
						&& !string.IsNullOrWhiteSpace(x.RefundAccountName);
				})
				.WithMessage("Incomplete bank information. Bank name, account number, and account name are all required if requesting a manual refund.");
		}
	}

	public class StaffCancelOrderRequestValidator : AbstractValidator<StaffCancelOrderRequest>
	{
		public StaffCancelOrderRequestValidator()
		{
			RuleFor(x => x.Note)
				.MaximumLength(1000).WithMessage("Note must not exceed 1000 characters.")
				.When(x => !string.IsNullOrWhiteSpace(x.Note));
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
