using PerfumeGPT.Application.DTOs.Requests.Imports;
using PerfumeGPT.Domain.Entities;

namespace PerfumeGPT.Application.Interfaces.Services
{
	public interface IBatchService
	{
		/// <summary>
		/// Creates batches for an import detail with validation.
		/// NOTE: Does NOT call SaveChanges - caller must save changes (transaction orchestrator).
		/// </summary>
		/// <param name="variantId">The variant ID for the batches</param>
		/// <param name="importDetailId">The import detail ID</param>
		/// <param name="batchRequests">List of batch creation requests</param>
		/// <returns>List of created batch entities (tracked but not saved)</returns>
		Task<List<Batch>> CreateBatchesAsync(Guid variantId, Guid importDetailId, List<CreateBatchRequest> batchRequests);

	/// <summary>
	/// Validates batch requests against business rules.
	/// </summary>
	/// <param name="batchRequests">List of batch requests to validate</param>
	/// <param name="expectedTotalQuantity">Expected total quantity to match</param>
	/// <returns>True if valid, false otherwise</returns>
	bool ValidateBatches(List<CreateBatchRequest> batchRequests, int expectedTotalQuantity);

	/// <summary>
	/// Deducts quantity from a batch's remaining quantity (used for order fulfillment).
	/// NOTE: Does NOT call SaveChanges - caller must save changes (transaction orchestrator).
	/// </summary>
	/// <param name="batchId">The batch ID to deduct from</param>
	/// <param name="quantity">The quantity to deduct</param>
	/// <returns>True if deduction was successful, false if insufficient quantity or batch not found</returns>
	Task<bool> DeductBatchAsync(Guid batchId, int quantity);

	/// <summary>
	/// Validates if sufficient batch quantity is available for a variant (checks non-expired batches).
	/// </summary>
	/// <param name="variantId">The variant ID to check</param>
	/// <param name="requiredQuantity">The quantity required</param>
	/// <returns>True if sufficient batch quantity available, false otherwise</returns>
	Task<bool> ValidateBatchAvailabilityAsync(Guid variantId, int requiredQuantity);

	/// <summary>
	/// Deducts quantity from batches for a variant (FIFO - oldest non-expired first).
	/// NOTE: Does NOT call SaveChanges - caller must save changes (transaction orchestrator).
	/// </summary>
	/// <param name="variantId">The variant ID to deduct from</param>
	/// <param name="quantity">The quantity to deduct</param>
	/// <returns>True if deduction was successful, false if insufficient quantity</returns>
	Task<bool> DeductBatchesByVariantAsync(Guid variantId, int quantity);
}


}
