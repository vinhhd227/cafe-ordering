using Mediator;
using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Api.UseCases.Common.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IMessage
{
  private const int SlowRequestThresholdMs = 500;

  public async ValueTask<TResponse> Handle(
    TRequest request,
    MessageHandlerDelegate<TRequest, TResponse> next,
    CancellationToken ct)
  {
    var requestName = typeof(TRequest).Name;

    logger.LogInformation("[START] {RequestName}", requestName);

    var sw = Stopwatch.StartNew();

    try
    {
      var response = await next(request, ct);
      sw.Stop();

      if (sw.ElapsedMilliseconds > SlowRequestThresholdMs)
      {
        logger.LogWarning(
          "[SLOW] {RequestName} took {ElapsedMs}ms",
          requestName,
          sw.ElapsedMilliseconds);
      }
      else
      {
        logger.LogInformation(
          "[END] {RequestName} completed in {ElapsedMs}ms",
          requestName,
          sw.ElapsedMilliseconds);
      }

      return response;
    }
    catch (Exception ex)
    {
      sw.Stop();

      logger.LogError(
        ex,
        "[ERROR] {RequestName} failed after {ElapsedMs}ms",
        requestName,
        sw.ElapsedMilliseconds);

      throw;
    }
  }
}
