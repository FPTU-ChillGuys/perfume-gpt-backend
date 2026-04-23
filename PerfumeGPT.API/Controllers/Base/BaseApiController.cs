using Microsoft.AspNetCore.Mvc;
using PerfumeGPT.Application.DTOs.Responses.Base;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.RegularExpressions;

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

		protected (Guid UserId, string? Role) GetCurrentUserContext() =>
		(GetCurrentUserId(), User.FindFirstValue("role") ?? User.FindFirstValue(ClaimTypes.Role));


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
		/// Kiểm tra danh sách (List, Array, IEnumerable) không được rỗng.
		/// </summary>
		protected ActionResult? ValidateNotEmptyCollection<T, TItem>(IEnumerable<TItem>? collection, string paramName)
		{
			if (collection == null || !collection.Any())
			{
				var resp = BaseResponse<T>.Fail($"Danh sách {paramName} không được để trống.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}
			return null;
		}


		/// <summary>
		/// Kiểm tra kiểu int (thường dùng cho ID) phải lớn hơn 0.
		/// </summary>
		protected ActionResult? ValidatePositiveInt(int value, string paramName) // Bỏ <T>
		{
			if (value <= 0)
			{
				// Dùng BaseResponse<object> cho mọi lỗi
				var resp = BaseResponse<object>.Fail($"{paramName} phải lớn hơn 0.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}
			return null;
		}

		/// <summary>
		/// Kiểm tra chuỗi string không được null hoặc rỗng.
		/// </summary>
		protected ActionResult? ValidateRequiredString(string? value, string paramName) // Bỏ <T>
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				var resp = BaseResponse<object>.Fail($"{paramName} không được để trống.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}
			return null;
		}

		// Regex kiểm tra số điện thoại Việt Nam (bắt đầu bằng 0, theo sau là 9 chữ số, và có các đầu số hợp lệ)
		private static readonly Regex VietnamPhoneRegex =
			new(@"^(0)(3[2-9]|5[6789]|7[06789]|8[0-9]|9[0-9])[0-9]{7}$", RegexOptions.Compiled);

		// Kiểm tra chuỗi phải là email hợp lệ hoặc số điện thoại Việt Nam hợp lệ
		protected ActionResult? ValidatePhoneOrEmail(string? value, string paramName)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				var resp = BaseResponse<object>.Fail($"{paramName} không được để trống.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}

			var isEmail = new EmailAddressAttribute().IsValid(value);
			var isPhone = VietnamPhoneRegex.IsMatch(value);

			if (!isEmail && !isPhone)
			{
				var resp = BaseResponse<object>.Fail($"{paramName} phải là email hoặc số điện thoại hợp lệ.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}

			return null;
		}
	}
}
