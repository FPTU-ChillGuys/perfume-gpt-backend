using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Responses.Base;
using System.Security.Claims;

namespace PerfumeGPT.API.Controllers.Base
{
	[ApiController]
	public abstract class BaseApiController : ControllerBase
	{
		/// <summary>
		/// Lấy UserId của user hiện tại từ token
		/// </summary> 
		protected Guid GetCurrentUserId()
		{
			var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
			return Guid.TryParse(userIdString, out var userId) ? userId : Guid.Empty;
		}

		/// <summary>
		/// Xử lý response cho các hàm có payload (generic)
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
		/// Xử lý response cho các hàm không có payload (non-generic)
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
	}
}
