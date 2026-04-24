using FluentValidation;
using PerfumeGPT.Application.DTOs.Requests.Payments;

namespace PerfumeGPT.Application.Validators.Payments
{
	public class RetryOrChangePaymentRequestValidator : AbstractValidator<RetryOrChangePaymentRequest>
	{
		public RetryOrChangePaymentRequestValidator()
		{
			// 1. Kiểm tra phương thức thanh toán mới (nếu có)
			RuleFor(x => x.NewPaymentMethod)
				.IsInEnum().WithMessage("Phương thức thanh toán không hợp lệ.")
				.When(x => x.NewPaymentMethod.HasValue);

			// 2. Chặn lỗi logic: Chọn trả Full nhưng lại đính kèm cổng Cọc
			RuleFor(x => x)
				.Must(x =>
				{
					// Nếu không đổi phương thức (Chỉ Retry) -> Bỏ qua
					if (!x.NewPaymentMethod.HasValue) return true;

					bool isCodOrStore = x.NewPaymentMethod.Value == Domain.Enums.PaymentMethod.CashOnDelivery ||
										x.NewPaymentMethod.Value == Domain.Enums.PaymentMethod.CashInStore;

					// Nếu KHÔNG PHẢI đơn COD/Tại quầy (nghĩa là trả Full), thì KHÔNG ĐƯỢC gửi NewDepositMethod
					if (!isCodOrStore)
						return !x.NewDepositMethod.HasValue;

					return true;
				})
				.WithMessage("Đơn hàng thanh toán toàn bộ không được đính kèm cổng thanh toán cọc.");

			// 3. Giới hạn các cổng cọc hợp lệ
			RuleFor(x => x.NewDepositMethod)
				.Must(gateway => !gateway.HasValue
					|| gateway == Domain.Enums.PaymentMethod.VnPay
					|| gateway == Domain.Enums.PaymentMethod.Momo
					|| gateway == Domain.Enums.PaymentMethod.PayOs)
				.WithMessage("Cổng thanh toán đặt cọc chỉ hỗ trợ VNPay, Momo hoặc PayOs.");
		}
	}
}
