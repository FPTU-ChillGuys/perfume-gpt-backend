using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Entities
{
	public class CashFlowLedger : BaseEntity<Guid>
	{
		protected CashFlowLedger() { }

		public DateTime TransactionDate { get; private set; } // Ngày phát sinh
		public decimal Amount { get; private set; } // +150000 (Thu) hoặc -30000 (Chi)
		public CashFlowType FlowType { get; private set; } // Enum: In (Thu), Out (Chi)
		public CashFlowCategory Category { get; private set; } // Enum: OrderPayment, Refund, ShippingFeeToGHN, SupplierPayment...

		// Soft Links (Lưu vết nguồn gốc dòng tiền)
		public Guid ReferenceId { get; private set; } // ID của Order, hoặc ShippingInfo, hoặc ImportTicket
		public string? ReferenceCode { get; private set; } // "ORD-123", "GHN-456" để UI hiển thị cho đẹp

		public string? Description { get; private set; } // "Khách thanh toán VNPay đơn ORD-123"

		public static CashFlowLedger CreateLog(
			Guid referenceId,
			string referenceCode,
			decimal amount,
			CashFlowType flowType,
			CashFlowCategory category,
			string description)
		{
			return new CashFlowLedger
			{
				TransactionDate = DateTime.UtcNow,
				Amount = amount, // Lệnh thu là +100000, lệnh chi là -50000
				FlowType = flowType,
				Category = category,
				ReferenceId = referenceId,
				ReferenceCode = referenceCode,
				Description = description
			};
		}
	}
}
