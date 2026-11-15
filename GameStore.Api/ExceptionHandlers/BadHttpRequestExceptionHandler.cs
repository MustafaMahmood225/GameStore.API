using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GameStore.Api.ExceptionHandlers
{
    // WHY: IExceptionHandler is the hook UseExceptionHandler() calls for every unhandled
    // exception. Each registered handler gets a chance to deal with it. A handler returns
    // true for "I dealt with it, stop here", or false for "not mine, ask the next one".
    // The primary constructor lets dependency injection supply a logger and the service
    // that writes problem-details responses.
    public class BadHttpRequestExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<BadHttpRequestExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync (HttpContext context, Exception exception, CancellationToken cancellationToken)
        {
            // WHY: We only handle one exception type. BadHttpRequestException means the
            // client sent something ASP.NET couldn't read (a bad number in the query
            // string, broken JSON). That is the client's mistake, not a server crash.
            // Returning false lets every other exception fall through to the default 500.
            if (exception is not BadHttpRequestException badRequest)
            {
                return false; // not mine, ask the next handler
            }
            // WHY: Log the real message for us. A Warning, not an Error, because the
            // server is working fine.
            logger.LogWarning("Bad request: {Message}", badRequest.Message);

            context.Response.StatusCode = StatusCodes.Status400BadRequest;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Title = "Invalid request",
                    Detail = "One or more request values are invalid.",
                    Status = StatusCodes.Status400BadRequest
                }


            });
        }
    }
}
