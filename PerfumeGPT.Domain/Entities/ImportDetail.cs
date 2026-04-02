using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ImportDetail : BaseEntity<Guid>
	{
		protected ImportDetail() { }

		public Guid ImportId { get; private set; }
		public Guid ProductVariantId { get; private set; }
		public int ExpectedQuantity { get; private set; }
		public decimal UnitPrice { get; private set; }
		public int RejectedQuantity { get; private set; } = 0;
		public string? Note { get; private set; }

		// Navigation properties
		public virtual ImportTicket ImportTicket { get; set; } = null!;
		public virtual ProductVariant ProductVariant { get; set; } = null!;
		public virtual ICollection<Batch> Batches { get; set; } = [];

		// Factory methods
		public static ImportDetail Create(ImportItemInfo item)
		{
			ValidateVariantId(item.VariantId);
			ValidateExpectedQuantity(item.Quantity);
			ValidateUnitPrice(item.UnitPrice);

			return new ImportDetail
			{
				ProductVariantId = item.VariantId,
				ExpectedQuantity = item.Quantity,
				UnitPrice = item.UnitPrice
			};
		}

		// Business logic methods
		public void UpdateExpected(ImportItemInfo item)
		{
			ValidateVariantId(item.VariantId);
			ValidateExpectedQuantity(item.Quantity);
			ValidateUnitPrice(item.UnitPrice);

			ProductVariantId = item.VariantId;
			ExpectedQuantity = item.Quantity;
			UnitPrice = item.UnitPrice;
		}

		public void Verify(DetailVerification detail)
		{
			if (detail.RejectedQuantity < 0)
				throw DomainException.BadRequest("Rejected quantity cannot be negative.");

			if (detail.RejectedQuantity > ExpectedQuantity)
				throw DomainException.BadRequest("Rejected quantity cannot exceed expected quantity.");

			RejectedQuantity = detail.RejectedQuantity;
			Note = string.IsNullOrWhiteSpace(detail.Note) ? null : detail.Note.Trim();
		}

		private static void ValidateVariantId(Guid productVariantId)
		{
			if (productVariantId == Guid.Empty)
				throw DomainException.BadRequest("Product variant ID is required.");
		}

		private static void ValidateExpectedQuantity(int expectedQuantity)
		{
			if (expectedQuantity <= 0)
				throw DomainException.BadRequest("Expected quantity must be greater than 0.");
		}

		private static void ValidateUnitPrice(decimal unitPrice)
		{
			if (unitPrice <= 0)
				throw DomainException.BadRequest("Unit price must be greater than 0.");
		}

		// Records
		public record ImportItemInfo(
			Guid VariantId,
			int Quantity,
			decimal UnitPrice
		);

		public record DetailVerification(
			int RejectedQuantity,
			string? Note
		);
	}
}
