using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.Application.Exceptions
{
	public class AppException : Exception
	{
		public ResponseErrorType ErrorType { get; }
		public List<string>? Errors { get; }

		public AppException(
			string message,
			ResponseErrorType errorType = ResponseErrorType.BadRequest,
			List<string>? errors = null,
			Exception? innerException = null) : base(message, innerException)
		{
			ErrorType = errorType;
			Errors = errors;
		}

		public static AppException NotFound(string message) =>
		new(message, ResponseErrorType.NotFound);

		public static AppException Forbidden(string message) =>
			new(message, ResponseErrorType.Forbidden);

		public static AppException Unauthorized(string message) =>
			new(message, ResponseErrorType.Unauthorized);

		public static AppException Conflict(string message) =>
			new(message, ResponseErrorType.Conflict);

		public static AppException BadRequest(string message, List<string>? errors = null) =>
			new(message, ResponseErrorType.BadRequest, errors);

		public static AppException Internal(string message) =>
			new(message, ResponseErrorType.InternalError);
	}
}
