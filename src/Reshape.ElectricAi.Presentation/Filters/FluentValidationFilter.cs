using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Reshape.ElectricAi.Presentation.Middleware;

namespace Reshape.ElectricAi.Presentation.Filters;

public sealed class FluentValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            var validator = serviceProvider.GetService(validatorType) as IValidator;
            if (validator is null)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            if (result.IsValid)
            {
                continue;
            }

            var details = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => string.IsNullOrEmpty(g.Key) ? "_" : g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            context.Result = new BadRequestObjectResult(
                ErrorEnvelope.WithDetails("validation-failed", "One or more validation errors occurred.", details));
            return;
        }

        await next();
    }
}
