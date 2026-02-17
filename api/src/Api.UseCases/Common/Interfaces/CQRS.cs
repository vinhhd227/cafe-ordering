// Api.UseCases/Common/Interfaces/CQRS.cs
using Mediator; // ✅ Dùng Mediator, không phải MediatR

namespace Api.UseCases.Common.Interfaces;

/// <summary>
/// Query marker interface - tương thích với Mediator
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Command marker interface - tương thích với Mediator
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse>
{
}

/// <summary>
/// Query handler - tương thích với Mediator
/// </summary>
public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, TResponse>
  where TQuery : IQuery<TResponse>
{
}

/// <summary>
/// Command handler - tương thích với Mediator
/// </summary>
public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, TResponse>
  where TCommand : ICommand<TResponse>
{
}
