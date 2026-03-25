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
		public static Supplier Create(string name, string contactEmail, string phone, string address)
		{
			return new Supplier
			{
				Name = NormalizeName(name),
				ContactEmail = NormalizeEmail(contactEmail),
				Phone = NormalizePhone(phone),
				Address = NormalizeAddress(address)
			};
		}

		public void UpdateDetails(string name, string contactEmail, string phone, string address)
		{
			Name = NormalizeName(name);
			ContactEmail = NormalizeEmail(contactEmail);
			Phone = NormalizePhone(phone);
			Address = NormalizeAddress(address);
		}

		// Business logic methods
		public static string NormalizeName(string name)
		{
			var normalized = name?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
				throw DomainException.BadRequest("Supplier name is required.");
			return normalized;
		}

		public static string NormalizeEmail(string email)
		{
			var normalized = email?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
				throw DomainException.BadRequest("Supplier contact email is required.");
			return normalized;
		}

		public static string NormalizePhone(string phone)
		{
			var normalized = phone?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
				throw DomainException.BadRequest("Supplier phone is required.");
			if (!PhoneRegex.IsMatch(normalized))
				throw DomainException.BadRequest("Invalid phone number format.");
			return normalized;
		}

		public static string NormalizeAddress(string address)
		{
			var normalized = address?.Trim() ?? string.Empty;
			if (string.IsNullOrWhiteSpace(normalized))
				throw DomainException.BadRequest("Supplier address is required.");
			return normalized;
		}

		public static void EnsureCanBeDeleted(bool hasImportTickets)
		{
			if (hasImportTickets)
				throw DomainException.BadRequest("Cannot delete supplier with associated import tickets.");
		}

		private static readonly Regex PhoneRegex = new("^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$", RegexOptions.Compiled);
	}
}
