using Microsoft.EntityFrameworkCore;
using PerfumeGPT.Application.DTOs.Responses.Base;
using PerfumeGPT.Application.Exceptions;
using PerfumeGPT.Domain.Exceptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PerfumeGPT.API.Middlewares
{
	public class GlobalExceptionMiddleware
	{
		private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
		{
			Converters =
			{
				new JsonStringEnumConverter()
			}
		};

		private readonly RequestDelegate _next;
		private readonly ILogger<GlobalExceptionMiddleware> _logger;

		public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
		{
			_next = next;
			_logger = logger;
		}

		public async Task InvokeAsync(HttpContext context)
		{
			try
			{
				await _next(context);
			}
			catch (Exception ex)
			{
				if (ex is AppException || ex is DomainException)
				{
					_logger.LogWarning("Business error: {Message}", ex.Message);
				}
				else
				{
					_logger.LogError(ex, "System failure: {Message}", ex.Message);
				}
				await HandleExceptionAsync(context, ex);
			}
		}

		private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
		{
			context.Response.ContentType = "application/json";

			var (statusCode, response) = exception switch
			{
				AppException ex => (
					(int)ex.ErrorType,
					BaseResponse<object?>.Fail(ex.Message, ex.ErrorType, ex.Errors)),

				DomainException ex => (
					(int)ex.ErrorType,
					 BaseResponse<object?>.Fail(ex.Message, (ResponseErrorType)(int)ex.ErrorType)),

				UnauthorizedAccessException => (
					(int)ResponseErrorType.Unauthorized,
					BaseResponse<object?>.Fail("You are not authorized.", ResponseErrorType.Unauthorized)),

				DbUpdateConcurrencyException => (
					(int)ResponseErrorType.Conflict,
					BaseResponse<object?>.Fail(
				  "Dữ liệu bạn đang thao tác vừa được cập nhật bởi một người dùng khác. Vui lòng làm mới trang và thử lại.",
				  ResponseErrorType.Conflict)),

				_ => (
					(int)ResponseErrorType.InternalError,
					 BaseResponse<object?>.Fail("An unexpected server error occurred.", ResponseErrorType.InternalError))
			};

			context.Response.StatusCode = statusCode;
			await context.Response.WriteAsJsonAsync(response, JsonOptions);
		}
	}
}
