using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Zapper.ApiCommon.Errors;

public sealed class UnhandledExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService problemDetailsService;

    private readonly ILogger<UnhandledExceptionHandler> logger;

    public UnhandledExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<UnhandledExceptionHandler> logger)
    {
        this.problemDetailsService = problemDetailsService;
        this.logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An unhandled exception stopped the request");

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception
        });
    }
}
