using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class ContactAddress : BaseEntity<Guid>
	{
		protected ContactAddress() { }

		public string ContactName { get; private set; } = null!;
		public string ContactPhoneNumber { get; private set; } = null!;

		// Calculate Shipping fee based on Address
		public int DistrictId { get; private set; }
		public string DistrictName { get; private set; } = null!;
		public string WardCode { get; private set; } = null!;
		public string WardName { get; private set; } = null!;
		public string ProvinceName { get; private set; } = null!;

		// Full Address
		public string FullAddress { get; private set; } = null!;

		// Factory methods
		public static ContactAddress Create(ContactAddressPayload payload)
		{
			var address = new ContactAddress();
			address.UpdateAddress(payload);
			return address;
		}

		public void UpdateAddress(ContactAddressPayload payload)
		{
			if (string.IsNullOrWhiteSpace(payload.ContactName))
				throw DomainException.BadRequest("Contact name is required.");

			if (string.IsNullOrWhiteSpace(payload.ContactPhoneNumber))
				throw DomainException.BadRequest("Contact phone number is required.");

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

			ContactName = payload.ContactName.Trim();
			ContactPhoneNumber = payload.ContactPhoneNumber.Trim();
			DistrictId = payload.DistrictId;
			DistrictName = payload.DistrictName.Trim();
			WardCode = payload.WardCode.Trim();
			WardName = payload.WardName.Trim();
			ProvinceName = payload.ProvinceName.Trim();
			FullAddress = payload.FullAddress.Trim();
		}

		public void UpdateContactAddress(ContactAddressPayload payload)
		{
			UpdateAddress(payload);
		}

		// Records
		public record ContactAddressPayload
		{
			public required string ContactName { get; init; }
			public required string ContactPhoneNumber { get; init; }
			public required int DistrictId { get; init; }
			public required string DistrictName { get; init; }
			public required string WardCode { get; init; }
			public required string WardName { get; init; }
			public required string ProvinceName { get; init; }
			public required string FullAddress { get; init; }
		}
	}
}
