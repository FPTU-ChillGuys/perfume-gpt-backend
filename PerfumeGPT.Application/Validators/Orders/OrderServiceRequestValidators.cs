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
			 .GreaterThan(0).WithMessage("Số trang phải lớn hơn 0.");

			RuleFor(x => x.PageSize)
				.GreaterThan(0).WithMessage("Kích thước trang phải lớn hơn 0.")
				.LessThanOrEqualTo(50).WithMessage("Kích thước trang phải nhỏ hơn hoặc bằng 50.");

			RuleFor(x => x.SortOrder)
				.Must(order => string.Equals(order, "asc", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(order, "desc", StringComparison.OrdinalIgnoreCase))
			 .WithMessage("Thứ tự sắp xếp chỉ được là 'asc' hoặc 'desc'.");

			RuleFor(x => x)
				.Must(x => !x.FromDate.HasValue || !x.ToDate.HasValue || x.FromDate <= x.ToDate)
			   .WithMessage("Từ ngày phải nhỏ hơn hoặc bằng đến ngày.");

			RuleFor(x => x.SearchTerm)
			  .MaximumLength(200).WithMessage("Từ khóa tìm kiếm không được vượt quá 200 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.SearchTerm));
		}
	}

	public class CreateOrderRequestValidator : AbstractValidator<CreateOrderRequest>
	{
		public CreateOrderRequestValidator()
		{
			RuleFor(x => x.ItemIds)
				.Must(ids => ids.Distinct().Count() == ids.Count)
			 .WithMessage("Không cho phép ID sản phẩm trùng lặp.")
				.When(x => x.ItemIds != null);

			RuleFor(x => x.ExpectedTotalPrice)
				.GreaterThanOrEqualTo(0).WithMessage("Tổng tiền kỳ vọng phải lớn hơn hoặc bằng 0.")
				.When(x => x.ExpectedTotalPrice.HasValue);

			RuleFor(x => x.DeliveryMethod)
				.IsInEnum().WithMessage("Phương thức giao hàng không hợp lệ.");

			RuleFor(x => x.Payment)
			 .NotNull().WithMessage("Thông tin thanh toán là bắt buộc.");

			RuleFor(x => x.Payment.Method)
			 .IsInEnum().WithMessage("Phương thức thanh toán không hợp lệ.");

			RuleFor(x => x.Payment.DepositGateway)
				.Must(gateway => !gateway.HasValue
					|| gateway == Domain.Enums.PaymentMethod.VnPay
					|| gateway == Domain.Enums.PaymentMethod.Momo
					|| gateway == Domain.Enums.PaymentMethod.PayOs)
				.WithMessage("Cổng thanh toán đặt cọc chỉ hỗ trợ VNPay, Momo hoặc PayOs.");

			RuleFor(x => x)
				.Must(x => x.DeliveryMethod != Domain.Enums.DeliveryMethod.Delivery || x.SavedAddressId.HasValue || x.Recipient != null)
			   .WithMessage("Đơn giao hàng cần có ID địa chỉ đã lưu hoặc thông tin người nhận.");

			RuleFor(x => x.VoucherCode)
			   .MaximumLength(50).WithMessage("Mã giảm giá không được vượt quá 50 ký tự.")
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
			 .NotNull().WithMessage("Thông tin thanh toán là bắt buộc.");

			RuleFor(x => x.Payment.Method)
			 .IsInEnum().WithMessage("Phương thức thanh toán không hợp lệ.");

			RuleFor(x => x.Payment.DepositGateway)
				.Must(gateway => !gateway.HasValue
					|| gateway == Domain.Enums.PaymentMethod.CashInStore
					|| gateway == Domain.Enums.PaymentMethod.VnPay
					|| gateway == Domain.Enums.PaymentMethod.Momo
					|| gateway == Domain.Enums.PaymentMethod.PayOs)
				.WithMessage("Cổng thanh toán đặt cọc chỉ hỗ trợ CashInStore, VNPay, Momo hoặc PayOs.");

			RuleFor(x => x)
				.Must(x => x.IsPickupInStore || x.Recipient != null)
				.WithMessage("Thông tin người nhận là bắt buộc khi không nhận tại cửa hàng.");

			RuleFor(x => x.VoucherCode)
			   .MaximumLength(50).WithMessage("Mã giảm giá không được vượt quá 50 ký tự.")
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
			 .NotEmpty().WithMessage("Variant ID là bắt buộc.");

			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
		}
	}

	public class PreviewOrderRequestValidator : AbstractValidator<PreviewOrderRequest>
	{
		public PreviewOrderRequestValidator()
		{
			RuleFor(x => x.BarCodes)
			   .NotEmpty().WithMessage("Bắt buộc có ít nhất một mã vạch.");

			RuleForEach(x => x.BarCodes)
				.NotEmpty().WithMessage("Mã vạch không được để trống.");

			RuleFor(x => x.VoucherCode)
			   .MaximumLength(50).WithMessage("Mã giảm giá không được vượt quá 50 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.VoucherCode));
		}
	}

	public class UserCancelOrderRequestValidator : AbstractValidator<UserCancelOrderRequest>
	{
		public UserCancelOrderRequestValidator()
		{
			RuleFor(x => x.Reason).IsInEnum().WithMessage("Lý do hủy không hợp lệ.");

			RuleFor(x => x.RefundBankName)
			 .MaximumLength(255).WithMessage("Tên ngân hàng hoàn tiền không được vượt quá 255 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundBankName));

			RuleFor(x => x.RefundAccountNumber)
			  .MaximumLength(50).WithMessage("Số tài khoản hoàn tiền không được vượt quá 50 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.RefundAccountNumber));

			RuleFor(x => x.RefundAccountName)
			  .MaximumLength(255).WithMessage("Tên chủ tài khoản hoàn tiền không được vượt quá 255 ký tự.")
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
			   .WithMessage("Thông tin ngân hàng chưa đầy đủ. Khi yêu cầu hoàn tiền thủ công, bắt buộc có tên ngân hàng, số tài khoản và tên chủ tài khoản.");
		}
	}

	public class StaffCancelOrderRequestValidator : AbstractValidator<StaffCancelOrderRequest>
	{
		public StaffCancelOrderRequestValidator()
		{
			RuleFor(x => x.Note)
			   .MaximumLength(1000).WithMessage("Ghi chú không được vượt quá 1000 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.Note));
		}
	}

	public class FulfillOrderRequestValidator : AbstractValidator<FulfillOrderRequest>
	{
		public FulfillOrderRequestValidator()
		{
			RuleFor(x => x.Items)
			 .NotEmpty().WithMessage("Danh sách sản phẩm hoàn tất đơn là bắt buộc.");

			RuleFor(x => x.Items)
				.Must(items => items.Select(i => i.OrderDetailId).Distinct().Count() == items.Count)
			   .WithMessage("Không cho phép OrderDetail ID trùng lặp trong danh sách hoàn tất đơn.");

			RuleForEach(x => x.Items)
				.SetValidator(new FulfillOrderItemRequestValidator());
		}
	}

	public class FulfillOrderItemRequestValidator : AbstractValidator<FulfillOrderItemRequest>
	{
		public FulfillOrderItemRequestValidator()
		{
			RuleFor(x => x.OrderDetailId)
				.NotEmpty().WithMessage("Order detail ID là bắt buộc.");

			RuleFor(x => x.ScannedBatchCode)
			 .NotEmpty().WithMessage("Mã lô đã quét là bắt buộc.");

			RuleFor(x => x.Quantity)
				.GreaterThan(0).WithMessage("Số lượng phải lớn hơn 0.");
		}
	}

	public class SwapDamagedStockRequestValidator : AbstractValidator<SwapDamagedStockRequest>
	{
		public SwapDamagedStockRequestValidator()
		{
			RuleFor(x => x.DamagedReservationId)
			 .NotEmpty().WithMessage("Damaged reservation ID là bắt buộc.");

			RuleFor(x => x.DamageNote)
				.MaximumLength(1000).WithMessage("Ghi chú hư hỏng không được vượt quá 1000 ký tự.")
				.When(x => !string.IsNullOrWhiteSpace(x.DamageNote));
		}
	}
}
