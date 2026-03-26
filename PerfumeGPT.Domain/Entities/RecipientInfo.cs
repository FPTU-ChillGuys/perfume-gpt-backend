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
		public static RecipientInfo Create(
			Guid orderId,
			string recipientName,
			string recipientPhoneNumber,
			int districtId,
			string districtName,
			string wardCode,
			string wardName,
			string provinceName,
			string fullAddress)
		{
			if (orderId == Guid.Empty)
				throw DomainException.BadRequest("Order ID is required.");

			var recipientInfo = new RecipientInfo
			{
				OrderId = orderId
			};

			recipientInfo.UpdateRecipient(
				recipientName,
				recipientPhoneNumber,
				districtId,
				districtName,
				wardCode,
				wardName,
				provinceName,
				fullAddress);

			return recipientInfo;
		}

		public void UpdateRecipient(
			string recipientName,
			string recipientPhoneNumber,
			int districtId,
			string districtName,
			string wardCode,
			string wardName,
			string provinceName,
			string fullAddress)
		{
			if (string.IsNullOrWhiteSpace(recipientName))
				throw DomainException.BadRequest("Recipient name is required.");

			if (string.IsNullOrWhiteSpace(recipientPhoneNumber))
				throw DomainException.BadRequest("Recipient phone number is required.");

			if (districtId <= 0)
				throw DomainException.BadRequest("District ID must be greater than 0.");

			if (string.IsNullOrWhiteSpace(districtName))
				throw DomainException.BadRequest("District name is required.");

			if (string.IsNullOrWhiteSpace(wardCode))
				throw DomainException.BadRequest("Ward code is required.");

			if (string.IsNullOrWhiteSpace(wardName))
				throw DomainException.BadRequest("Ward name is required.");

			if (string.IsNullOrWhiteSpace(provinceName))
				throw DomainException.BadRequest("Province name is required.");

			if (string.IsNullOrWhiteSpace(fullAddress))
				throw DomainException.BadRequest("Full address is required.");

			RecipientName = recipientName.Trim();
			RecipientPhoneNumber = recipientPhoneNumber.Trim();
			DistrictId = districtId;
			DistrictName = districtName.Trim();
			WardCode = wardCode.Trim();
			WardName = wardName.Trim();
			ProvinceName = provinceName.Trim();
			FullAddress = fullAddress.Trim();
		}
	}
}
