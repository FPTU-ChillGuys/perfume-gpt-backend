namespace PerfumeGPT.Application.DTOs.Responses.Base
{
    public class BaseResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = "OK";
        public List<string>? Errors { get; set; }
        public ResponseErrorType ErrorType { get; set; } = ResponseErrorType.None;

        public static BaseResponse Ok(string message = "Success")
            => new() { Success = true, Message = message };

        public static BaseResponse Fail(string message, ResponseErrorType errorType = ResponseErrorType.InternalError, List<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors, ErrorType = errorType };
    }

    public class BaseResponse<T> : BaseResponse
    {
        public T? Payload { get; set; }

        public static BaseResponse<T> Ok(T data, string message = "Success")
            => new() { Success = true, Message = message, Payload = data };

        public static new BaseResponse<T> Fail(string message, ResponseErrorType errorType = ResponseErrorType.InternalError, List<string>? errors = null)
            => new() { Success = false, Message = message, Errors = errors, ErrorType = errorType };
    }
}
