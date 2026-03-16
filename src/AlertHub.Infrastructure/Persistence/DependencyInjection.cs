using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlertHub.Infrastructure.Persistence;

    public static class DependencyInjection
    {
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AlertHubDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("AlertHub"),
                sql => sql.MigrationsAssembly(typeof(AlertHubDbContext).Assembly.FullName))
                  );
            
            return services;
        }
    }

