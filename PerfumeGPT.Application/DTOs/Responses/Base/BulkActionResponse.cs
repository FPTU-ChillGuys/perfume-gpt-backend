namespace PerfumeGPT.Application.DTOs.Responses.Base
{
	// ==================== INTERNAL SERVICE LAYER ====================

	/// <summary>
	/// Internal response used by service methods to track bulk operation results.
	/// This is transformed into BulkActionResult for API responses.
	/// </summary>
	public class BulkActionResponse
	{
		public List<Guid> SucceededIds { get; set; } = [];
		public List<BulkActionError> FailedItems { get; set; } = [];

		public int TotalProcessed => SucceededIds.Count + FailedItems.Count;
		public bool HasError => FailedItems.Count != 0;
	}

	/// <summary>
	/// Represents a single error in a bulk operation (shared by all bulk types)
	/// </summary>
	public class BulkActionError
	{
		public Guid Id { get; set; }
		public string ErrorMessage { get; set; } = null!;
	}

	// ==================== API LAYER ====================

	/// <summary>
	/// Response wrapper that includes the main payload and optional bulk operation metadata.
	/// Used in API responses when operations involve multiple items that can partially succeed/fail.
	/// </summary>
	/// <typeparam name="T">The type of the main payload (e.g., Guid for created entity ID, string for messages)</typeparam>
	public class BulkActionResult<T>
	{
		public T? Data { get; set; }
		public BulkActionMetadata? Metadata { get; set; }

		public BulkActionResult(T data, BulkActionMetadata? metadata = null)
		{
			Data = data;
			Metadata = metadata;
		}
	}

	/// <summary>
	/// Metadata about bulk operations performed during the request.
	/// Contains aggregated information about all operations (e.g., Media Upload, Media Deletion).
	/// </summary>
	public class BulkActionMetadata
	{
		public List<BulkOperationResult> Operations { get; set; } = [];

		public bool HasPartialFailure => Operations.Any(o => o.HasError);
		public bool AllSucceeded => Operations.All(o => !o.HasError);
		public int TotalOperations => Operations.Sum(o => o.TotalProcessed);
		public int TotalSucceeded => Operations.Sum(o => o.SucceededCount);
		public int TotalFailed => Operations.Sum(o => o.FailedCount);
	}

	/// <summary>
	/// Result of a specific bulk operation (e.g., "Media Deletion", "Media Upload").
	/// Contains success/failure counts and detailed error information.
	/// </summary>
	public class BulkOperationResult
	{
		public string OperationName { get; set; } = null!;
		public int SucceededCount { get; set; }
		public int FailedCount { get; set; }
		public List<BulkActionError> Errors { get; set; } = [];

		public int TotalProcessed => SucceededCount + FailedCount;
		public bool HasError => Errors.Count > 0;

		/// <summary>
		/// Converts an internal BulkActionResponse to an API-friendly BulkOperationResult
		/// </summary>
		public static BulkOperationResult FromBulkActionResponse(string operationName, BulkActionResponse response)
		{
			return new BulkOperationResult
			{
				OperationName = operationName,
				SucceededCount = response.SucceededIds.Count,
				FailedCount = response.FailedItems.Count,
				Errors = response.FailedItems
			};
		}
	}
}
