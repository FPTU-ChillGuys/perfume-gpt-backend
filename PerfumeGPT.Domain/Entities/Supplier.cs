using PerfumeGPT.Domain.Commons;
using PerfumeGPT.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace PerfumeGPT.Domain.Entities
{
	public class Supplier : BaseEntity<int>
	{
		private Supplier() { }

		public string Name { get; private set; } = null!;
		public string ContactEmail { get; private set; } = null!;
		public string Phone { get; private set; } = null!;
		public string Address { get; private set; } = null!;

		// Navigation property
		public virtual ICollection<ImportTicket> ImportTickets { get; set; } = [];

		// Factory methods
		public static Supplier Create(SupplierPayload payload)
		{
			return new Supplier
			{
				Name = NormalizeName(payload.Name),
				ContactEmail = NormalizeEmail(payload.ContactEmail),
				Phone = NormalizePhone(payload.Phone),
				Address = NormalizeAddress(payload.Address)
			};
		}

		public void UpdateDetails(SupplierPayload payload)
		{
			Name = NormalizeName(payload.Name);
			ContactEmail = NormalizeEmail(payload.ContactEmail);
			Phone = NormalizePhone(payload.Phone);
			Address = NormalizeAddress(payload.Address);
		}

		// Business logic methods
		public static string NormalizeName(string name)
		{
			var normalized = name?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
             throw DomainException.BadRequest("Tên nhà cung cấp là bắt buộc.");
			return normalized;
		}

		public static string NormalizeEmail(string email)
		{
			var normalized = email?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
                throw DomainException.BadRequest("Email liên hệ nhà cung cấp là bắt buộc.");
			return normalized;
		}

		public static string NormalizePhone(string phone)
		{
			var normalized = phone?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
                throw DomainException.BadRequest("Số điện thoại nhà cung cấp là bắt buộc.");
			if (!PhoneRegex.IsMatch(normalized))
               throw DomainException.BadRequest("Định dạng số điện thoại không hợp lệ.");
			return normalized;
		}

		public static string NormalizeAddress(string address)
		{
			var normalized = address?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
              throw DomainException.BadRequest("Địa chỉ nhà cung cấp là bắt buộc.");
			return normalized;
		}

		private static readonly Regex PhoneRegex = new("^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$", RegexOptions.Compiled);

		// Records
		public record SupplierPayload
		{
			public required string Name { get; init; }
			public required string ContactEmail { get; init; }
			public required string Phone { get; init; }
			public required string Address { get; init; }
		}
	}
}
