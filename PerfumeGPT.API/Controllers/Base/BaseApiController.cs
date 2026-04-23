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
				var resp = BaseResponse<object>.Fail("Nội dung request không được để trống.", ResponseErrorType.BadRequest);
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
				var resp = BaseResponse<T>.Fail("Nội dung request không được để trống.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}
			return null;
		}

		//protected async Task<ActionResult?> ValidateRequestAsync<T>(IValidator<T> validator, T request)
		//{
		//	if (request == null)
		//	{
		//		var resp = BaseResponse<T>.Fail("Nội dung request không được để trống.", ResponseErrorType.BadRequest);
		//		return HandleResponse(resp);
		//	}

		//	var validationResult = await validator.ValidateAsync(request);
		//	if (!validationResult.IsValid)
		//	{
		//		var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
		//		var resp = BaseResponse<T>.Fail("Dữ liệu không hợp lệ.", ResponseErrorType.BadRequest, errors);
		//		return HandleResponse(resp);
		//	}

		//	return null;
		//}

		/// <summary>
		/// Kiểm tra kiểu int (thường dùng cho ID) phải lớn hơn 0.
		/// </summary>
		protected ActionResult? ValidatePositiveInt<T>(int value, string paramName)
		{
			if (value <= 0)
			{
				var resp = BaseResponse<T>.Fail($"{paramName} phải lớn hơn 0.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}
			return null;
		}

		/// <summary>
		/// Kiểm tra chuỗi string không được null hoặc rỗng.
		/// </summary>
		protected ActionResult? ValidateRequiredString<T>(string? value, string paramName)
		{
			if (string.IsNullOrWhiteSpace(value))
			{
				var resp = BaseResponse<T>.Fail($"{paramName} không được để trống.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}
			return null;
		}

		/// <summary>
		/// Kiểm tra Guid không được là Guid.Empty.
		/// </summary>
		protected ActionResult? ValidateNotEmptyGuid(Guid value, string paramName)
		{
			if (value == Guid.Empty)
			{
				var resp = BaseResponse<object>.Fail($"{paramName} không được bỏ trống hoặc không hợp lệ.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}
			return null;
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

		/// <summary>
		/// Validate Validator (Sửa lại để trả về BaseResponse<object> thay vì BaseResponse<T>)
		/// </summary>
		protected async Task<ActionResult?> ValidateRequestAsync<TRequest>(IValidator<TRequest> validator, TRequest request)
		{
			if (request == null)
			{
				var resp = BaseResponse<object>.Fail("Nội dung request không được để trống.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}

			var validationResult = await validator.ValidateAsync(request);
			if (!validationResult.IsValid)
			{
				var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
				var resp = BaseResponse<object>.Fail("Dữ liệu không hợp lệ.", ResponseErrorType.BadRequest, errors);
				return HandleResponse(resp);
			}

			return null;
		}

		protected ActionResult? ValidateEnum<TEnum>(TEnum? value, string paramName) where TEnum : struct, Enum
		{
			// Nếu giá trị là null (do user truyền sai kiểu chữ, hoặc không truyền gì nhưng tham số là bắt buộc)
			// Lưu ý: Nếu tham số của bạn cho phép null (optional), bạn nên kiểm tra value.HasValue trước khi gọi hàm này, 
			// hoặc thiết kế hàm này chỉ validate khi có giá trị.
			if (!value.HasValue)
			{
				// Trong ngữ cảnh này, do .NET gán null khi ép kiểu lỗi (như phân tích ở phiên trước)
				// Ta có thể bắt lỗi null ở đây.
				var resp = BaseResponse<object>.Fail($"Giá trị {paramName} không hợp lệ.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}

			// Nếu có giá trị, kiểm tra xem nó có thực sự nằm trong Enum không
			// (Phòng trường hợp user truyền số int không tồn tại trong Enum, VD: ?position=999)
			if (!Enum.IsDefined(typeof(TEnum), value.Value))
			{
				var resp = BaseResponse<object>.Fail($"Giá trị {paramName} không tồn tại trong hệ thống.", ResponseErrorType.BadRequest);
				return HandleResponse(resp);
			}

			return null;
		}
	}
}
