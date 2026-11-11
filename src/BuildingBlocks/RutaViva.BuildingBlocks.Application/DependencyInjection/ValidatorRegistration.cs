using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace RutaViva.BuildingBlocks.Application.DependencyInjection;

internal static class ValidatorRegistration
{
    public static void AddOnce(IServiceCollection services, Assembly assembly)
    {
        if (IsRegistered(services, assembly))
        {
            return;
        }

        services.AddValidatorsFromAssembly(assembly, ServiceLifetime.Scoped, includeInternalTypes: true);
    }

    private static bool IsRegistered(IServiceCollection services, Assembly assembly) =>
        services.Any(descriptor => IsValidatorOf(descriptor, assembly));

    private static bool IsValidatorOf(ServiceDescriptor descriptor, Assembly assembly) =>
        descriptor.ServiceType.IsGenericType
        && descriptor.ServiceType.GetGenericTypeDefinition() == typeof(IValidator<>)
        && descriptor.ImplementationType?.Assembly == assembly;
}
