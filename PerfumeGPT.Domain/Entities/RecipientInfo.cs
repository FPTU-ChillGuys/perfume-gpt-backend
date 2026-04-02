using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class RecipientInfo : BaseEntity<Guid>
	{
		protected RecipientInfo() { }

		public Guid OrderId { get; private set; }
		public string RecipientName { get; private set; } = null!;
		public string RecipientPhoneNumber { get; private set; } = null!;

		// Calculate Shipping fee based on Address
		public int DistrictId { get; private set; }
		public string DistrictName { get; private set; } = null!;
		public string WardCode { get; private set; } = null!;
		public string WardName { get; private set; } = null!;
		public string ProvinceName { get; private set; } = null!;

		// Recipient Full Address
		public string FullAddress { get; private set; } = null!;

		// Navigation property
		public virtual Order Order { get; set; } = null!;

		// Factory methods
		public static RecipientInfo Create(Guid orderId, RecipientPayload payload)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			var recipientInfo = new RecipientInfo
			{
				OrderId = orderId
			};

			recipientInfo.UpdateRecipient(payload);

			return recipientInfo;
		}

		public void UpdateRecipient(RecipientPayload payload)
		{
			if (string.IsNullOrWhiteSpace(payload.RecipientName))
				throw DomainException.BadRequest("Recipient name is required.");

			if (string.IsNullOrWhiteSpace(payload.RecipientPhoneNumber))
				throw DomainException.BadRequest("Recipient phone number is required.");

			if (payload.DistrictId <= 0)
				throw DomainException.BadRequest("District ID must be greater than 0.");

			if (string.IsNullOrWhiteSpace(payload.DistrictName))
				throw DomainException.BadRequest("District name is required.");

			if (string.IsNullOrWhiteSpace(payload.WardCode))
				throw DomainException.BadRequest("Ward code is required.");

			if (string.IsNullOrWhiteSpace(payload.WardName))
				throw DomainException.BadRequest("Ward name is required.");

			if (string.IsNullOrWhiteSpace(payload.ProvinceName))
				throw DomainException.BadRequest("Province name is required.");

			if (string.IsNullOrWhiteSpace(payload.FullAddress))
				throw DomainException.BadRequest("Full address is required.");

			RecipientName = payload.RecipientName.Trim();
			RecipientPhoneNumber = payload.RecipientPhoneNumber.Trim();
			DistrictId = payload.DistrictId;
			DistrictName = payload.DistrictName.Trim();
			WardCode = payload.WardCode.Trim();
			WardName = payload.WardName.Trim();
			ProvinceName = payload.ProvinceName.Trim();
			FullAddress = payload.FullAddress.Trim();
		}

		// Records
		public record RecipientPayload
		{
			public required string RecipientName { get; init; }
			public required string RecipientPhoneNumber { get; init; }
			public required int DistrictId { get; init; }
			public required string DistrictName { get; init; }
			public required string WardCode { get; init; }
			public required string WardName { get; init; }
			public required string ProvinceName { get; init; }
			public required string FullAddress { get; init; }
		}
	}
}
