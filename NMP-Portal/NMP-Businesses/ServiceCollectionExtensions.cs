using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NMP.Core.Attributes;
using System.Reflection;
namespace NMP.Businesses;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RegisterBusinesses(this IServiceCollection services)
    {
        return AddBusinesses(services);
    }
    private static IServiceCollection AddBusinesses(this IServiceCollection services)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();

        var typesWithAttribute = assembly.GetTypes()
            .Where(type => type.GetCustomAttribute<BusinessAttribute>() != null)
            .ToList();

        foreach (var type in typesWithAttribute)
        {
            var attribute = type.GetCustomAttribute<BusinessAttribute>();
            var interfaces = type.GetInterfaces();

            if (interfaces.Length > 0)
            {
                foreach (var item in interfaces)
                {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                    services.Add(new ServiceDescriptor(item, type, attribute.Lifetime));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                }
            }
            else
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                services.Add(new ServiceDescriptor(type, type, attribute.Lifetime));
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            }
        }

        return services;
    }
}
