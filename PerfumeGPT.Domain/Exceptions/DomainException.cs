using PerfumeGPT.Domain.Enums;

namespace PerfumeGPT.Domain.Exceptions
{
	public class DomainException : Exception
	{
		public DomainErrorType ErrorType { get; }

		public DomainException(string message, DomainErrorType errorType = DomainErrorType.BadRequest, Exception? innerException = null)
			: base(message, innerException)
		{
			ErrorType = errorType;
		}

		public static DomainException NotFound(string message) =>
		new(message, DomainErrorType.NotFound);

		public static DomainException Forbidden(string message) =>
			new(message, DomainErrorType.Forbidden);

		public static DomainException Conflict(string message) =>
			new(message, DomainErrorType.Conflict);

		public static DomainException BadRequest(string message) =>
			new(message, DomainErrorType.BadRequest);
	}
}
