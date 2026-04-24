using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using PerfumeGPT.Application.DTOs.Responses.Base;

namespace PerfumeGPT.API.Filters
{
    public class RequestValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!context.ModelState.IsValid)
            {
                var modelErrors = GetModelStateErrors(context.ModelState);
                context.Result = CreateBadRequestResult("Dữ liệu không hợp lệ.", modelErrors);
                return;
            }

            foreach (var parameter in context.ActionDescriptor.Parameters.OfType<ControllerParameterDescriptor>())
            {
                if (!context.ActionArguments.TryGetValue(parameter.Name, out var argument))
                {
                    continue;
                }

                if (argument is null)
                {
                    if (IsRequiredBodyParameter(parameter))
                    {
                        context.Result = CreateBadRequestResult("Nội dung request không được để trống.", null);
                        return;
                    }

                    continue;
                }

                if (argument is Guid guidValue && guidValue == Guid.Empty)
                {
                    context.Result = CreateBadRequestResult($"Tham số '{parameter.Name}' không hợp lệ (không được để trống).", null);
                    return;
                }

                var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
                if (serviceProvider.GetService(validatorType) is not IValidator validator)
                {
                    continue;
                }

                var validationResult = await validator.ValidateAsync(new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                        .Select(e => e.ErrorMessage)
                        .Where(e => !string.IsNullOrWhiteSpace(e))
                        .Distinct()
                        .ToList();

                    context.Result = CreateBadRequestResult("Dữ liệu không hợp lệ.", errors);
                    return;
                }
            }

            await next();
        }

        private static bool IsRequiredBodyParameter(ControllerParameterDescriptor parameter)
        {
            var bindingSource = parameter.BindingInfo?.BindingSource;
            if (bindingSource != BindingSource.Body)
            {
                return false;
            }

            var parameterInfo = parameter.ParameterInfo;
            if (parameterInfo.HasDefaultValue)
            {
                return false;
            }

            return !IsNullable(parameterInfo.ParameterType);
        }

        private static bool IsNullable(Type type)
        {
            if (!type.IsValueType)
            {
                return true;
            }

            return Nullable.GetUnderlyingType(type) != null;
        }

        private static List<string>? GetModelStateErrors(ModelStateDictionary modelState)
        {
            var errors = modelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => string.IsNullOrWhiteSpace(e.ErrorMessage) ? "Giá trị không hợp lệ." : e.ErrorMessage)
                .Distinct()
                .ToList();

            return errors.Count == 0 ? null : errors;
        }

        private static JsonResult CreateBadRequestResult(string message, List<string>? errors)
        {
            var response = BaseResponse<object>.Fail(message, ResponseErrorType.BadRequest, errors);
            return new JsonResult(response)
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
    }
}
