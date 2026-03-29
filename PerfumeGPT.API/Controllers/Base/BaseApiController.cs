using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Responses.Base;
using System.Security.Claims;

namespace PerfumeGPT.API.Controllers.Base
{
	[ApiController]
	public abstract class BaseApiController : ControllerBase
	{
		protected Guid GetCurrentUserId()
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
		}

		/// <summary>
		/// Handle response for generic BaseResponse<T>. This centralizes the logic for interpreting the success status and error types, ensuring consistent API responses across all controllers that inherit from BaseApiController.
		/// </summary>
		protected ActionResult HandleResponse<T>(BaseResponse<T> result)
		{
			if (result.Success)
			{
				return Ok(result);
			}

			return result.ErrorType switch
			{
				ResponseErrorType.NotFound => NotFound(result),
				ResponseErrorType.BadRequest => BadRequest(result),
				ResponseErrorType.Conflict => Conflict(result),
				ResponseErrorType.Unauthorized => Unauthorized(result),
				ResponseErrorType.Forbidden => StatusCode(403, result),
				ResponseErrorType.InternalError => StatusCode(500, result),
				_ => BadRequest(result)
			};
		}

		/// <summary>
		/// Handle response for non-generic BaseResponse. Useful for endpoints that don't return data but still want to indicate success or failure with a message.
		/// </summary>
		protected ActionResult HandleResponse(BaseResponse result)
		{
			if (result.Success)
			{
				return Ok(result);
			}

			return result.ErrorType switch
			{
				ResponseErrorType.NotFound => NotFound(result),
				ResponseErrorType.BadRequest => BadRequest(result),
				ResponseErrorType.Conflict => Conflict(result),
				ResponseErrorType.Unauthorized => Unauthorized(result),
				ResponseErrorType.Forbidden => StatusCode(403, result),
				ResponseErrorType.InternalError => StatusCode(500, result),
				_ => BadRequest(result)
			};
		}

		/// <summary>
		/// Reusable helper to validate request bodies are not null.
		/// Returns null when the request is valid; otherwise returns an ActionResult produced by HandleResponse.
		/// Usage: var validation = ValidateRequestBody(request); if (validation != null) return validation;
		/// </summary>
		protected ActionResult? ValidateRequestBody(object? request)
		{
			if (request == null)
			{
				var resp = BaseResponse<object>.Fail("Request body cannot be null.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}
			return null;
		}

		/// <summary>
		/// Reusable helper to validate request bodies are not null.
		/// Returns null when the request is valid; otherwise returns an ActionResult produced by HandleResponse.
		/// </summary>
		protected ActionResult? ValidateRequestBody<T>(object? request)
		{
			if (request == null)
			{
				var resp = BaseResponse<T>.Fail("Request body cannot be null.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}
			return null;
		}

		protected async Task<ActionResult?> ValidateRequestAsync<T>(IValidator<T> validator, T request)
		{
			if (request == null)
			{
				var resp = BaseResponse<T>.Fail("Request body cannot be null.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}

			var validationResult = await validator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				var resp = BaseResponse<T>.Fail("Validation failed.", ResponseErrorType.BadRequest, errors);
				return HandleResponse(resp);
			}

			return null;
		}

	}
}
