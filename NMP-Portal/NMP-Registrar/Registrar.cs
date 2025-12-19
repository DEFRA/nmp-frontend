using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NMP.Businesses;
using NMP.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NMP.Registrar
{
    public static class Registrar 
    {
        public static IServiceCollection RegisterDependencies(IServiceCollection services)
        {
            services.RegisterBusinesses();
            services.RegisterServices();
            return services;
        }        
    }
}
