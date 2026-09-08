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
        // Validated at host start, so a clone with no signing key configured refuses to boot
        // with a message naming the setting instead of failing on the first sign-in.
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("Jwt"))
            .ValidateOnStart();

        services.AddSingleton<IValidateOptions<JwtSettings>, JwtSettingsValidator>();

        services.AddDbContext<DbContext, AlertHubDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("AlertHub"),
                sql => sql.MigrationsAssembly(typeof(AlertHubDbContext).Assembly.FullName))
                  );

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer();

        // Configured through the options pipeline rather than inline, because the inline form
        // needed its own configuration.GetSection("Jwt").Get<JwtSettings>() at registration
        // time — a second bind that ran before ValidateOnStart and so could never benefit from
        // it. Taking IOptions<JwtSettings> here means the bearer scheme and TokenService read
        // one validated instance, and a bad key is reported at startup by the validator.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>>((bearer, jwt) =>
            {
                var settings = jwt.Value;

                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = settings.Issuer,
                    ValidAudience = settings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(settings.SecretKey)),
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
