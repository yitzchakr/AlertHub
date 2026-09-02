using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.OnCall;
using AlertHub.Application.Interfaces;
using AlertHub.Infrastructure.Auth;
using AlertHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using AlertHub.Application.Settings;
using System.Text;

namespace AlertHub.Infrastructure;

    public static class DependencyInjection
    {
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        services.AddDbContext<DbContext, AlertHubDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("AlertHub"),
                sql => sql.MigrationsAssembly(typeof(AlertHubDbContext).Assembly.FullName))
                  );

        var jwtSettings = configuration.GetSection("Jwt").Get<JwtSettings>()!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                RoleClaimType = "role",
                NameClaimType = "sub"
            };
          
        });

        services.AddAuthorization(options =>
        {
            options.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .Build();
        });

        services.AddHttpContextAccessor();
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<ITokenService, TokenService>();
        // One instance per scope serving both interfaces. Registering the two separately
        // would hand out two objects, so a worker's SetTenant would update an instance the
        // DbContext never reads — and the worker would then find nothing, silently.
        services.AddScoped<ScopedTenantContext>();
        services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<ScopedTenantContext>());
        services.AddScoped<ITenantScope>(sp => sp.GetRequiredService<ScopedTenantContext>());

        // Live in the Application assembly (it references EF Core) but are registered
        // here alongside the other scoped services.
        services.AddScoped<IOnCallResolver, OnCallResolver>();
        services.AddScoped<IEscalationTargetResolver, EscalationTargetResolver>();

        return services;
    }
    }

