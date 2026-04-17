using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Receipt : BaseEntity<Guid>, IHasCreatedAt
	{
		protected Receipt() { }

		public Guid TransactionId { get; private set; }
		public string ReceiptNumber { get; private set; } = null!;

		// Navigation properties
		public virtual PaymentTransaction PaymentTransaction { get; set; } = null!;

		// IHasCreatedAt implementation
		public DateTime CreatedAt { get; set; }

		// Factory methods
		public static Receipt Create(Guid transactionId)
		{
			var receiptNumber = GenerateReceiptNumber();
			if (transactionId == Guid.Empty)
				throw DomainException.BadRequest("Transaction ID là bắt buộc.");

			if (string.IsNullOrWhiteSpace(receiptNumber))
				throw DomainException.BadRequest("Số biên nhận là bắt buộc.");

			return new Receipt
			{
				TransactionId = transactionId,
				ReceiptNumber = receiptNumber.Trim()
			};
		}

		private static string GenerateReceiptNumber() => $"RCP-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper()}";
	}
}
