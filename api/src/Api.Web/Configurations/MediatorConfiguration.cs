using Api.UseCases.Common.Behaviors;
using FluentValidation;

namespace Api.Web.Configurations;

public static class MediatorConfiguration
{
  public static IServiceCollection AddAppMediator(this IServiceCollection services)
  {
    // Mediator (source generator - tự scan assembly)
    services.AddMediator(options =>
    {
      options.ServiceLifetime = ServiceLifetime.Scoped;
    });

    // FluentValidation — scan validators từ UseCases assembly
    // Thay bằng bất kỳ type nào trong UseCases assembly
    services.AddValidatorsFromAssemblyContaining(typeof(ValidationBehavior<,>));

    return services;
  }
}
