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
					if (!x.NewPaymentMethod.HasValue) return true;

					// Vì ở tầng Validator chúng ta không có "order.Type" để biết là Online hay Offline, 
					// nên ta tạm thời cho phép cả COD và CashInStore mang theo DepositMethod qua cửa.
					// Service ở bên trong sẽ kiểm tra lại sự hợp lệ (Online/Offline) một lần nữa.
					bool isCodOrStore = x.NewPaymentMethod.Value == Domain.Enums.PaymentMethod.CashOnDelivery ||
										x.NewPaymentMethod.Value == Domain.Enums.PaymentMethod.CashInStore;

					// Nếu chắc chắn trả Full (như VNPay, MoMo) thì tuyệt đối cấm đính kèm DepositMethod
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
					|| gateway == Domain.Enums.PaymentMethod.PayOs
					|| gateway == Domain.Enums.PaymentMethod.CashInStore) // BỔ SUNG TIỀN MẶT TẠI QUẦY
				.WithMessage("Cổng thanh toán đặt cọc chỉ hỗ trợ VNPay, MoMo, PayOS hoặc Tiền mặt tại quầy."); // CẬP NHẬT LỜI NHẮN
		}
	}
}
