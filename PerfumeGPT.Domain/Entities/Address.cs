using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Commons.Audits;
using PerfumeGPT.Domain.Exceptions;

namespace PerfumeGPT.Domain.Entities
{
	public class Address : BaseEntity<Guid>, IHasTimestamps
	{
		public Guid UserId { get; set; }
		public string RecipientName { get; set; } = string.Empty;
		public string RecipientPhoneNumber { get; set; } = string.Empty;
		public string Street { get; set; } = string.Empty;
		public string Ward { get; set; } = string.Empty;
		public string District { get; set; } = string.Empty;
		public string City { get; set; } = string.Empty;
		public string WardCode { get; set; } = null!;
		public int DistrictId { get; set; }
		public int ProvinceId { get; set; }
		public bool IsDefault { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public virtual User User { get; set; } = null!;

		public bool IsOwnedBy(Guid userId) => UserId == userId;
		public bool CanBeDeleted() => !IsDefault;

		public void EnsureOwnedBy(Guid requestUserId)
		{
			if (!IsOwnedBy(requestUserId))
				throw DomainException.Forbidden("Address does not belong to this user");
		}

		public void EnsureCanBeDeleted()
		{
			if (!CanBeDeleted())
				throw DomainException.BadRequest(
					"Cannot delete default address. Please set another address as default first.");
		}

		public void EnsureNotAlreadyDefault()
		{
			if (IsDefault)
				throw DomainException.BadRequest("Address is already set as default.");
		}

		public void SetAsDefault() => IsDefault = true;
		public void UnsetDefault() => IsDefault = false;
	}
}