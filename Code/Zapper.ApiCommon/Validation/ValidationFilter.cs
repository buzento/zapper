using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Zapper.ApiCommon.Validation;

public sealed class ValidationFilter<TValidator> : IEndpointFilter
    where TValidator : IValidator
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var validator = ActivatorUtilities.CreateInstance<TValidator>(context.HttpContext.RequestServices);

        var request = context.Arguments.FirstOrDefault(argument =>
            argument is not null && validator.CanValidateInstancesOfType(argument.GetType()));

        if (request is null)
        {
            return await next(context);
        }

        var result = await validator.ValidateAsync(new ValidationContext<object>(request));

        if (result.IsValid)
        {
            return await next(context);
        }

        var errors = result.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(failure => failure.ErrorMessage).ToArray());

        return TypedResults.ValidationProblem(errors);
    }
}
