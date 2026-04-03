using PerfumeGPT.Application.DTOs.Requests.Orders;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IContactAddressService
	{
		Task<ContactAddress> CreateContactAddressAsync(ContactAddressInformation? request, Guid? savedAddressId, Guid? customerId = null);
		Task<ContactAddress> UpdateContactAddressAsync(ContactAddress existingRecipient, ContactAddressInformation? request, Guid? savedAddressId, Guid userId);
		Task<ContactAddressInformation> ResolveContactAddressDataAsync(ContactAddressInformation? contactInfo, Guid? savedAddressId, Guid? customerId);
	}
}
