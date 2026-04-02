using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Address : BaseEntity<Guid>, IHasTimestamps
	{
		private Address() { }

		public Guid UserId { get; private set; }
		public string RecipientName { get; private set; } = string.Empty;
		public string RecipientPhoneNumber { get; private set; } = string.Empty;
		public string Street { get; private set; } = string.Empty;
		public string Ward { get; private set; } = string.Empty;
		public string District { get; private set; } = string.Empty;
		public string City { get; private set; } = string.Empty;
		public string WardCode { get; private set; } = null!;
		public int DistrictId { get; private set; }
		public int ProvinceId { get; private set; }
		public bool IsDefault { get; private set; }

		// IHasTimestamps implementation
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }

		// Navigation property
		public virtual User User { get; set; } = null!;

		// Factory methods
		public static Address CreateForUser(Guid userId, AddressDetails details, bool isDefault)
		{
			return new Address
			{
				UserId = userId,
				RecipientName = details.RecipientName,
				RecipientPhoneNumber = details.RecipientPhoneNumber,
				Street = details.Street,
				Ward = details.Ward,
				District = details.District,
				City = details.City,
				WardCode = details.WardCode,
				DistrictId = details.DistrictId,
				ProvinceId = details.ProvinceId,
				IsDefault = isDefault
			};
		}

		public void UpdateDetails(AddressDetails details)
		{
			RecipientName = details.RecipientName;
			RecipientPhoneNumber = details.RecipientPhoneNumber;
			Street = details.Street;
			Ward = details.Ward;
			District = details.District;
			City = details.City;
			WardCode = details.WardCode;
			DistrictId = details.DistrictId;
			ProvinceId = details.ProvinceId;
		}

		// Business logic methods
		public void EnsureOwnedBy(Guid userId)
		{
			if (UserId != userId)
				throw DomainException.Forbidden("Address does not belong to this user.");
		}

		public void EnsureNotAlreadyDefault()
		{
			if (IsDefault)
				throw DomainException.BadRequest("Address is already set as default.");
		}

		public void SetAsDefault() => IsDefault = true;
		public void UnsetDefault() => IsDefault = false;

		// DTOs
		public record AddressDetails(
			string RecipientName,
			string RecipientPhoneNumber,
			string Street,
			string Ward,
			string District,
			string City,
			string WardCode,
			int DistrictId,
			int ProvinceId
		);
	}
}