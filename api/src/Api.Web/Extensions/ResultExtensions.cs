  namespace Api.Web.Extensions;

  /// <summary>
  ///   Map Ardalis.Result sang FastEndpoints HTTP response
  /// </summary>
  public static class ResultExtensions
  {
    /// <summary>
    ///   Send response dựa trên Result status
    /// </summary>
    public static async Task SendResultAsync<TResponse>(
      this IEndpoint ep,
      Result<TResponse> result,
      CancellationToken ct = default)
    {
      switch (result.Status)
      {
        case ResultStatus.Ok:
          await ep.HttpContext.Response.SendAsync(result.Value, cancellation: ct);
          break;

        case ResultStatus.Created:
          await ep.HttpContext.Response.SendAsync(result.Value, StatusCodes.Status201Created, cancellation: ct);
          break;

        case ResultStatus.NotFound:
          await ep.HttpContext.Response.SendNotFoundAsync(ct);
          break;

        case ResultStatus.Invalid:
          var errors = result.ValidationErrors
            .Select(e => new { e.Identifier, e.ErrorMessage })
            .ToList();
          ep.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
          await ep.HttpContext.Response.WriteAsJsonAsync(new { Errors = errors }, ct);
          break;

        case ResultStatus.Conflict:
          ep.HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
          await ep.HttpContext.Response.WriteAsJsonAsync(
            new { result.Errors }, ct);
          break;

        case ResultStatus.Forbidden:
          await ep.HttpContext.Response.SendForbiddenAsync(ct);
          break;

        case ResultStatus.Unauthorized:
          await ep.HttpContext.Response.SendUnauthorizedAsync(ct);
          break;

        default:
          ep.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
          await ep.HttpContext.Response.WriteAsJsonAsync(
            new { result.Errors }, ct);
          break;
      }
    }

    /// <summary>
    ///   Send response cho Result không có value (commands)
    /// </summary>
    public static async Task SendResultAsync(
      this IEndpoint ep,
      Result result,
      CancellationToken ct = default)
    {
      switch (result.Status)
      {
        case ResultStatus.Ok:
          await ep.HttpContext.Response.SendNoContentAsync(ct);
          break;

        case ResultStatus.NotFound:
          await ep.HttpContext.Response.SendNotFoundAsync(ct);
          break;

        case ResultStatus.Invalid:
          var errors = result.ValidationErrors
            .Select(e => new { e.Identifier, e.ErrorMessage })
            .ToList();
          ep.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
          await ep.HttpContext.Response.WriteAsJsonAsync(new { Errors = errors }, ct);
          break;

        case ResultStatus.Conflict:
          ep.HttpContext.Response.StatusCode = StatusCodes.Status409Conflict;
          await ep.HttpContext.Response.WriteAsJsonAsync(
            new { result.Errors }, ct);
          break;

        case ResultStatus.Forbidden:
          await ep.HttpContext.Response.SendForbiddenAsync(ct);
          break;

        case ResultStatus.Unauthorized:
          await ep.HttpContext.Response.SendUnauthorizedAsync(ct);
          break;

        default:
          ep.HttpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
          await ep.HttpContext.Response.WriteAsJsonAsync(
            new { result.Errors }, ct);
          break;
      }
    }
  }
