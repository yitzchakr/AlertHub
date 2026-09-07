using AlertHub.Application.Settings;
using AlertHub.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AlertHub.Application.Tests;

public class JwtSettingsValidatorTests
{
    private static JwtSettings Valid() => new()
    {
        SecretKey = new string('x', 64),
        Issuer = "AlertHub",
        Audience = "AlertHub",
        AccessTokenExpirationMinutes = 15,
    };

    private static ValidateOptionsResult Validate(JwtSettings settings) =>
        new JwtSettingsValidator().Validate(Options.DefaultName, settings);

    [Fact]
    public void A_complete_configuration_passes()
    {
        Assert.True(Validate(Valid()).Succeeded);
    }

    // The message is the whole point of the check: whoever hits this has just cloned the repo
    // and needs to be told where the key goes, not merely that one is missing.
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void A_missing_key_is_refused_and_says_where_to_put_one(string? key)
    {
        var settings = Valid();
        settings.SecretKey = key!;

        var result = Validate(settings);

        Assert.True(result.Failed);
        Assert.Contains("Jwt:SecretKey", result.FailureMessage);
        Assert.Contains("user-secrets", result.FailureMessage);
    }

    [Fact]
    public void A_key_too_short_for_HS256_is_refused()
    {
        var settings = Valid();
        settings.SecretKey = new string('x', 31);

        var result = Validate(settings);

        Assert.True(result.Failed);
        Assert.Contains("31 bytes", result.FailureMessage);
    }

    // 32 bytes is the boundary, not a value below it, so the check must admit exactly 32.
    [Fact]
    public void A_key_of_exactly_the_minimum_length_is_accepted()
    {
        var settings = Valid();
        settings.SecretKey = new string('x', 32);

        Assert.True(Validate(settings).Succeeded);
    }

    [Fact]
    public void A_blank_issuer_or_audience_is_refused()
    {
        var noIssuer = Valid();
        noIssuer.Issuer = "";
        var noAudience = Valid();
        noAudience.Audience = "";

        Assert.Contains("Jwt:Issuer", Validate(noIssuer).FailureMessage);
        Assert.Contains("Jwt:Audience", Validate(noAudience).FailureMessage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void A_non_positive_token_lifetime_is_refused(int minutes)
    {
        var settings = Valid();
        settings.AccessTokenExpirationMinutes = minutes;

        Assert.Contains("AccessTokenExpirationMinutes", Validate(settings).FailureMessage);
    }

    // Nothing reads RefreshTokenExpirationDays, so validating it would advertise a control
    // that does not exist. If a handler ever starts honouring it, this test should be deleted
    // and the validator taught about it.
    [Fact]
    public void An_unset_refresh_token_lifetime_is_not_treated_as_a_failure()
    {
        var settings = Valid();
        settings.RefreshTokenExpirationDays = 0;

        Assert.True(Validate(settings).Succeeded);
    }
}

/// <summary>
/// The validator only earns its keep if the composition root actually runs it at startup.
/// Dropping the .ValidateOnStart() call would leave every unit test above passing while a
/// keyless deployment booted happily and failed on the first sign-in, so the wiring is
/// asserted through the real AddInfrastructure rather than trusted.
/// </summary>
public class JwtStartupValidationTests
{
    private static IConfiguration Configuration(bool withSecretKey)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:AlertHub"] = "Server=none;Database=none;Trusted_Connection=True;",
            ["Jwt:Issuer"] = "tests",
            ["Jwt:Audience"] = "tests",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
        };

        if (withSecretKey) settings["Jwt:SecretKey"] = new string('x', 64);

        return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    }

    private static ServiceProvider Container(bool withSecretKey)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(Configuration(withSecretKey));
        return services.BuildServiceProvider();
    }

    /// <summary>Exactly what the host itself invokes before it starts anything.</summary>
    private static void RunStartupValidation(ServiceProvider provider) =>
        provider.GetRequiredService<IStartupValidator>().Validate();

    [Fact]
    public void A_host_with_no_signing_key_refuses_to_start()
    {
        using var provider = Container(withSecretKey: false);

        var error = Assert.Throws<OptionsValidationException>(() => RunStartupValidation(provider));

        Assert.Contains("Jwt:SecretKey", error.Message);
    }

    // The paired positive: without it, the throw above would still be satisfied by an
    // unrelated failure such as the email settings.
    [Fact]
    public void A_host_with_a_signing_key_starts()
    {
        using var provider = Container(withSecretKey: true);

        RunStartupValidation(provider);
    }
}
