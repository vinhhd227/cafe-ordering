using Mediator;
using FluentValidation;

namespace Api.UseCases.Common.Behaviors;

public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
  : IPipelineBehavior<TRequest, TResponse>
  where TRequest : IMessage
{
  public async ValueTask<TResponse> Handle(
    TRequest request,
    MessageHandlerDelegate<TRequest, TResponse> next,
    CancellationToken ct)
  {
    if (!validators.Any())
    {
      return await next(request, ct);
    }

    var context = new ValidationContext<TRequest>(request);

    var validationResults = await Task.WhenAll(
      validators.Select(v => v.ValidateAsync(context, ct)));

    var failures = validationResults
      .SelectMany(r => r.Errors)
      .Where(f => f is not null)
      .ToList();

    if (failures.Count == 0)
    {
      return await next(request, ct);
    }

    var resultErrors = failures
      .Select(f => new ValidationError(
        f.ErrorMessage,
        f.PropertyName,
        f.ErrorCode,
        ValidationSeverity.Error))
      .ToList();

    if (typeof(TResponse).IsGenericType &&
        typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
    {
      var resultType = typeof(TResponse).GetGenericArguments()[0];
      var invalidMethod = typeof(Result<>)
        .MakeGenericType(resultType)
        .GetMethod(nameof(Result.Invalid), new[] { typeof(List<ValidationError>) });

      return (TResponse)invalidMethod!.Invoke(null, new object[] { resultErrors })!;
    }

    if (typeof(TResponse) == typeof(Result))
    {
      return (TResponse)(object)Result.Invalid(resultErrors);
    }

    throw new ValidationException(failures);
  }
}
