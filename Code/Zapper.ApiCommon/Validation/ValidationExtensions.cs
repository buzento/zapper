using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Zapper.ApiCommon.Validation;

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithValidation<TValidator>(this RouteHandlerBuilder builder)
        where TValidator : IValidator =>
        builder.AddEndpointFilter<ValidationFilter<TValidator>>();
}
