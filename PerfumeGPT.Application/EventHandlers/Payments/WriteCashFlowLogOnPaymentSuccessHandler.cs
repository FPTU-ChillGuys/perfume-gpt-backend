using MediatR;
using PerfumeGPT.Application.Interfaces.Repositories;
using PerfumeGPT.Application.Interfaces.Repositories.Commons;
using PerfumeGPT.Domain.Entities;
using PerfumeGPT.Domain.Enums;
using PerfumeGPT.Domain.Events.Payments;

namespace PerfumeGPT.Application.EventHandlers.Payments
{
	public sealed class WriteCashFlowLogOnPaymentSuccessHandler : INotificationHandler<PaymentSuccessDomainEvent>
	{
		private readonly IOrderRepository _orderRepository;
		private readonly IGenericRepository<CashFlowLedger> _cashFlowLedgerRepository;

		public WriteCashFlowLogOnPaymentSuccessHandler(
			IOrderRepository orderRepository,
			IGenericRepository<CashFlowLedger> cashFlowLedgerRepository)
		{
			_orderRepository = orderRepository;
			_cashFlowLedgerRepository = cashFlowLedgerRepository;
		}

		public async Task Handle(PaymentSuccessDomainEvent notification, CancellationToken cancellationToken)
		{
			var order = await _orderRepository.GetOrderForPaymentSuccessLogAsync(notification.OrderId);
			if (order is null)
			{
				return;
			}

			var payment = order.PaymentTransactions.FirstOrDefault(p => p.Id == notification.PaymentTransactionId);
			if (payment is null)
			{
				return;
			}

			var customerName = string.IsNullOrWhiteSpace(order.Customer?.FullName)
				? "Khách vãng lai"
				: order.Customer.FullName;

			// 1. Khai báo các biến tùy biến theo loại giao dịch
			string description;
			CashFlowType flowType;
			CashFlowCategory category;

			// 2. Phân loại giao dịch (Dựa vào TransactionType có sẵn trong Entity)
			if (payment.TransactionType == TransactionType.Payment)
			{
				// Xử lý luồng THU TIỀN
				flowType = CashFlowType.In;
				category = CashFlowCategory.OrderPayment;
				description = $"Thu tiền đơn hàng {order.Code}. Khách: {customerName}. Hình thức: {payment.Method}.";
			}
			else if (payment.TransactionType == TransactionType.Refund)
			{
				// Xử lý luồng HOÀN TIỀN (Chi tiền)
				flowType = CashFlowType.Out;
				category = CashFlowCategory.Refund;

				// Amount của Refund bạn đang lưu là số âm (VD: -50000)
				// Hàm Math.Abs sẽ hiển thị số dương trong chuỗi log cho đẹp (VD: Hoàn 50,000đ)
				description = $"Hoàn tiền đơn hàng {order.Code} ({Math.Abs(payment.Amount):N0}đ). Khách: {customerName}. Hình thức: {payment.Method}.";

				if (!string.IsNullOrWhiteSpace(payment.GatewayTransactionNo))
				{
					description += $" Mã GD: {payment.GatewayTransactionNo}";
				}
			}
			else
			{
				// Bỏ qua các loại giao dịch không liên quan đến dòng tiền (nếu có)
				return;
			}

			// 3. Khởi tạo Log (Bạn cần update hàm CreateLog này trong Entity CashFlowLedger để nhận đủ tham số)
			var log = CashFlowLedger.CreateLog(
				referenceId: order.Id,
				referenceCode: order.Code,
				amount: payment.Amount, // Amount của Refund là số âm, rất chuẩn cho kế toán
				flowType: flowType,
				category: category,
				description: description
			);

			await _cashFlowLedgerRepository.AddAsync(log);
		}
	}
}
