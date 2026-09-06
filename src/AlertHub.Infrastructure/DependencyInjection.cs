using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.Escalations;
using AlertHub.Application.Features.Notifications;
using AlertHub.Infrastructure.Email;
using AlertHub.Infrastructure.BackgroundServices;
using AlertHub.Application.Features.OnCall;
using AlertHub.Application.Interfaces;
using AlertHub.Infrastructure.Auth;
using AlertHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
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

        services.Configure<EscalationSettings>(configuration.GetSection("Escalation"));
        services.AddEmailNotifications(configuration);
        services.AddScoped<IIncidentEscalator, IncidentEscalator>();
        services.AddScoped<EscalationScanner>();
        services.AddHostedService<EscalationWorker>();

        return services;
    }

    /// <summary>
    /// Mail always resolves through RedirectingEmailSender, whether or not a redirect is
    /// configured. Wiring it unconditionally means there is no safety to switch on and none
    /// to forget: with no address configured the decorator simply passes messages through.
    /// </summary>
    private static IServiceCollection AddEmailNotifications(
        this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection("Email");

        services.AddOptions<EmailSettings>()
            .Bind(section)
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<EmailSettings>, EmailSettingsValidator>();

        services.AddScoped<SmtpEmailSender>();
        services.AddScoped<IEmailSender>(sp => new RedirectingEmailSender(
            sp.GetRequiredService<SmtpEmailSender>(),
            sp.GetRequiredService<IOptions<EmailSettings>>(),
            sp.GetRequiredService<ILogger<RedirectingEmailSender>>()));

        if (section.GetValue<bool>("Enabled"))
        {
            services.AddScoped<INotificationSender, EmailNotificationSender>();
        }
        else
        {
            services.AddScoped<INotificationSender, LoggingNotificationSender>();
        }

        services.AddHostedService<EmailModeAnnouncer>();

        return services;
    }
}
