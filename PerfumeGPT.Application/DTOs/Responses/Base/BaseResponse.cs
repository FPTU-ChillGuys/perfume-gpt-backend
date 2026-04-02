using System.Text.Json.Serialization;

namespace PerfumeGPT.Application.DTOs.Responses.Base
{
	public record BaseResponse
	{
		public bool Success { get; init; } = true;
		public string Message { get; init; } = "OK";

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public List<string>? Errors { get; init; }

		[JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
		public ResponseErrorType? ErrorType { get; init; }

		public static BaseResponse Ok(string message = "Success")
			=> new() { Success = true, Message = message };

		public static BaseResponse Fail(string message, ResponseErrorType errorType = ResponseErrorType.InternalError, List<string>? errors = null)
			=> new() { Success = false, Message = message, Errors = errors, ErrorType = errorType };
	}

	public record BaseResponse<T> : BaseResponse
	{
		public T? Payload { get; init; }

		public static BaseResponse<T> Ok(T data, string message = "Success")
			=> new() { Success = true, Message = message, Payload = data };

		public static new BaseResponse<T> Fail(string message, ResponseErrorType errorType = ResponseErrorType.InternalError, List<string>? errors = null)
			=> new() { Success = false, Message = message, Errors = errors, ErrorType = errorType };
	}
}
