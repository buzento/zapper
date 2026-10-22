using Zapper.ApiCommon.Managers.ErrorMessage;
using Zapper.Domain.Error;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Zapper.ApiCommon.Errors;

public static class ProblemDetailsCustomizer
{
    public static void Customize(ProblemDetailsContext context)
    {
        var problem = context.ProblemDetails;
        var statusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        var manager = context.HttpContext.RequestServices.GetRequiredService<IErrorMessageManager>();
        var logger = context.HttpContext.RequestServices
            .GetRequiredService<ILogger<ProblemDetailsCustomizerLogCategory>>();

        var status = manager.GetStatusByStatusCode(statusCode);

        if (!problem.Extensions.TryGetValue("code", out var existing) || existing is null)
        {
            problem.Extensions["code"] = status.Code;
        }

        var code = (string)problem.Extensions["code"]!;

        problem.Title = status.Title;
        problem.Detail = GetProblemDetail(code, status, manager, logger);

        if (problem is HttpValidationProblemDetails validation)
        {
            foreach (var field in validation.Errors.Keys.ToList())
            {
                validation.Errors[field] = validation.Errors[field]
                    .Select(fieldCode => GetProblemDetail(fieldCode, status, manager, logger))
                    .ToArray();
            }
        }
    }

    private static string GetProblemDetail(
        string code,
        ErrorStatusEntity status,
        IErrorMessageManager manager,
        ILogger logger)
    {
        if (code == status.Code)
        {
            return status.Detail;
        }

        var message = manager.GetMessageByCode(code);

        if (message is not null)
        {
            return message.Detail;
        }

        logger.LogError("An operation's code {Code} has no message row", code);

        return status.Detail;
    }
}

#pragma warning disable S2094 // ILogger<T> needs a type, and ProblemDetailsCustomizer is static, BackendCode.md section 15
internal sealed class ProblemDetailsCustomizerLogCategory;
#pragma warning restore S2094
