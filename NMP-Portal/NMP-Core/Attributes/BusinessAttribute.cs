using Microsoft.Extensions.DependencyInjection;

namespace NMP.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class BusinessAttribute : Attribute
    {
        public ServiceLifetime Lifetime { get; }

        public BusinessAttribute(ServiceLifetime lifetime = ServiceLifetime.Scoped)
        {
            Lifetime = lifetime;
        }
    }
}
