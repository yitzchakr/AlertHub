using AlertHub.Application.Features.EscalationPolicies;
using AlertHub.Application.Features.Escalations;
using AlertHub.Application.Features.Notifications;
using AlertHub.Application.Interfaces;
using AlertHub.Domain.Entities;
using AlertHub.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace AlertHub.Application.Tests;

/// <summary>
/// The redirect is the safety net that keeps development from paging real people, so it is
/// tested as pure logic with no SMTP anywhere — which is the reason it lives in a decorator
/// rather than inside the transport.
/// </summary>
public class RedirectingEmailSenderTests
{
    private const string TestInbox = "dev-inbox@localhost";

    private static readonly EmailMessage Original = new(
        "dana@acme.test", "Dana Responder", "[P1 Critical] Payments API — tier 1", "Body text.");

    private static (RedirectingEmailSender Sender, CapturingEmailSender Inner) Build(string? redirectTo)
    {
        var inner = new CapturingEmailSender();
        var sender = new RedirectingEmailSender(
            inner,
            Options.Create(new EmailSettings { RedirectAllTo = redirectTo }),
            NullLogger<RedirectingEmailSender>.Instance);

        return (sender, inner);
    }

    [Fact]
    public async Task With_no_redirect_configured_the_message_passes_through_untouched()
    {
        var (sender, inner) = Build(redirectTo: null);

        await sender.SendAsync(Original, CancellationToken.None);

        var sent = Assert.Single(inner.Sent);
        Assert.Equal(Original, sent);
        Assert.Null(sent.RedirectedFrom);
    }

    // An empty string is what a half-removed config line leaves behind. It must mean "no
    // redirect" rather than "redirect to nowhere", which would fail every send.
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task An_absent_redirect_address_means_no_redirect(string? redirectTo)
    {
        var (sender, inner) = Build(redirectTo);

        await sender.SendAsync(Original, CancellationToken.None);

        Assert.Equal("dana@acme.test", Assert.Single(inner.Sent).ToAddress);
    }

    // The whole point: with a redirect configured, the real address must not reach the
    // transport at all.
    [Fact]
    public async Task With_a_redirect_configured_nothing_is_addressed_to_the_real_recipient()
    {
        var (sender, inner) = Build(TestInbox);

        await sender.SendAsync(Original, CancellationToken.None);

        var sent = Assert.Single(inner.Sent);
        Assert.Equal(TestInbox, sent.ToAddress);
        Assert.NotEqual("dana@acme.test", sent.ToAddress);
    }

    [Fact]
    public async Task A_redirected_message_still_says_who_it_was_for()
    {
        var (sender, inner) = Build(TestInbox);

        await sender.SendAsync(Original, CancellationToken.None);

        var sent = Assert.Single(inner.Sent);
        Assert.StartsWith("[DEV -> dana@acme.test]", sent.Subject);
        Assert.Contains("Dana Responder <dana@acme.test>", sent.Body);
        Assert.Equal("dana@acme.test", sent.RedirectedFrom);
    }

    [Fact]
    public async Task A_redirected_message_keeps_its_original_subject_and_body()
    {
        var (sender, inner) = Build(TestInbox);

        await sender.SendAsync(Original, CancellationToken.None);

        var sent = Assert.Single(inner.Sent);
        Assert.Contains("[P1 Critical] Payments API — tier 1", sent.Subject);
        Assert.Contains("Body text.", sent.Body);
    }

    [Fact]
    public async Task Every_recipient_of_a_fan_out_lands_in_the_one_inbox()
    {
        var (sender, inner) = Build(TestInbox);

        foreach (var address in new[] { "dana@acme.test", "eli@acme.test", "avery@acme.test" })
            await sender.SendAsync(Original with { ToAddress = address }, CancellationToken.None);

        Assert.Equal(3, inner.Sent.Count);
        Assert.All(inner.Sent, m => Assert.Equal(TestInbox, m.ToAddress));
        // Still three distinct messages, so fan-out stays visible while testing.
        Assert.Equal(3, inner.Sent.Select(m => m.RedirectedFrom).Distinct().Count());
    }
}

public class EmailNotificationSenderTests
{
    private static EscalationNotification ANotification(params EscalationTargetDto[] recipients) =>
        new(Guid.NewGuid(), "Checkout returning 500s", IncidentSeverity.P1_Critical,
            Guid.NewGuid(), "Payments API", 2, recipients);

    private static EscalationTargetDto Dana(EscalationTargetSource source = EscalationTargetSource.OnCall) =>
        new(Guid.NewGuid(), "Dana Responder", "dana@acme.test", source);

    private static (EmailNotificationSender Sender, CapturingEmailSender Inner) Build()
    {
        var inner = new CapturingEmailSender();
        return (new EmailNotificationSender(inner, NullLogger<EmailNotificationSender>.Instance), inner);
    }

    [Fact]
    public async Task One_message_is_sent_per_recipient()
    {
        var (sender, inner) = Build();
        var notification = ANotification(
            Dana(),
            new EscalationTargetDto(Guid.NewGuid(), "Eli Responder", "eli@acme.test", EscalationTargetSource.Role));

        await sender.SendAsync(notification, CancellationToken.None);

        Assert.Equal(2, inner.Sent.Count);
        Assert.Contains(inner.Sent, m => m.ToAddress == "dana@acme.test");
        Assert.Contains(inner.Sent, m => m.ToAddress == "eli@acme.test");
    }

    // Reaching nobody is a coverage gap, not an error, and there is no one to mail.
    [Fact]
    public async Task Reaching_nobody_sends_nothing()
    {
        var (sender, inner) = Build();

        await sender.SendAsync(ANotification(), CancellationToken.None);

        Assert.Empty(inner.Sent);
    }

    [Fact]
    public async Task The_subject_carries_severity_service_and_tier()
    {
        var (sender, inner) = Build();

        await sender.SendAsync(ANotification(Dana()), CancellationToken.None);

        var subject = Assert.Single(inner.Sent).Subject;
        Assert.Contains("P1 Critical", subject);
        Assert.Contains("Payments API", subject);
        Assert.Contains("tier 2", subject);
        Assert.Contains("Checkout returning 500s", subject);
    }

    [Theory]
    [InlineData(EscalationTargetSource.OnCall, "on call")]
    [InlineData(EscalationTargetSource.Role, "your role")]
    [InlineData(EscalationTargetSource.DirectUser, "names you directly")]
    public async Task The_body_explains_why_this_person_was_paged(
        EscalationTargetSource source, string expected)
    {
        var (sender, inner) = Build();

        await sender.SendAsync(ANotification(Dana(source)), CancellationToken.None);

        Assert.Contains(expected, Assert.Single(inner.Sent).Body);
    }

    // The escalator counts on this: a swallowed failure would stamp the escalation as sent
    // and nobody would ever be paged.
    [Fact]
    public async Task A_transport_failure_propagates()
    {
        var inner = new CapturingEmailSender { ThrowOnSend = new InvalidOperationException("smtp down") };
        var sender = new EmailNotificationSender(inner, NullLogger<EmailNotificationSender>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => sender.SendAsync(ANotification(Dana()), CancellationToken.None));
    }
}

public class EmailSettingsValidatorTests
{
    private static readonly EmailSettingsValidator Validator = new();

    private static bool IsValid(EmailSettings settings) =>
        Validator.Validate(null, settings).Succeeded;

    // The misconfiguration that must never be discovered at send time: a typo here would
    // otherwise fall through and mail real people.
    [Theory]
    [InlineData("not-an-address")]
    [InlineData("dev-inbox@")]
    [InlineData("@localhost")]
    public void A_malformed_redirect_address_fails_startup(string redirect)
    {
        Assert.False(IsValid(new EmailSettings { RedirectAllTo = redirect }));
    }

    [Fact]
    public void A_valid_redirect_address_is_accepted()
    {
        Assert.True(IsValid(new EmailSettings { RedirectAllTo = "dev-inbox@localhost" }));
    }

    [Fact]
    public void No_redirect_at_all_is_accepted()
    {
        Assert.True(IsValid(new EmailSettings { RedirectAllTo = null }));
    }

    [Fact]
    public void Smtp_settings_are_only_enforced_once_email_is_enabled()
    {
        var broken = new EmailSettings { Enabled = false, Smtp = { Host = "", Port = 0 } };

        Assert.True(IsValid(broken));
        Assert.False(IsValid(new EmailSettings { Enabled = true, Smtp = { Host = "", Port = 0 } }));
    }

    [Fact]
    public void An_enabled_sender_needs_a_valid_from_address()
    {
        Assert.False(IsValid(new EmailSettings { Enabled = true, From = "nonsense" }));
    }
}

internal sealed class CapturingEmailSender : IEmailSender
{
    public List<EmailMessage> Sent { get; } = [];
    public Exception? ThrowOnSend { get; set; }

    public Task SendAsync(EmailMessage message, CancellationToken ct)
    {
        if (ThrowOnSend is not null)
            throw ThrowOnSend;

        Sent.Add(message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// The classes above are only safe if the container actually wires them in that order, and
/// if the committed development configuration actually asks for a redirect.
/// </summary>
public class EmailWiringTests
{
    private static ServiceProvider Build(params (string Key, string? Value)[] overrides)
    {
        var settings = new Dictionary<string, string?>
        {
            ["ConnectionStrings:AlertHub"] = "Server=none;Database=none;Trusted_Connection=True;",
            ["Jwt:SecretKey"] = new string('x', 64),
            ["Jwt:Issuer"] = "tests",
            ["Jwt:Audience"] = "tests",
            ["Jwt:AccessTokenExpirationMinutes"] = "15",
        };

        foreach (var (key, value) in overrides)
            settings[key] = value;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
        return services.BuildServiceProvider();
    }

    [Fact]
    public void With_email_off_escalations_are_logged_rather_than_emailed()
    {
        using var provider = Build(("Email:Enabled", "false"));
        using var scope = provider.CreateScope();

        Assert.IsType<LoggingNotificationSender>(
            scope.ServiceProvider.GetRequiredService<INotificationSender>());
    }

    [Fact]
    public void With_email_on_escalations_are_emailed()
    {
        using var provider = Build(("Email:Enabled", "true"));
        using var scope = provider.CreateScope();

        Assert.IsType<EmailNotificationSender>(
            scope.ServiceProvider.GetRequiredService<INotificationSender>());
    }

    // The safety net is only a safety net if it is unconditionally in the chain. Resolving
    // the transport directly must be impossible through the interface.
    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    public void Mail_always_resolves_through_the_redirecting_sender(string emailEnabled)
    {
        using var provider = Build(("Email:Enabled", emailEnabled));
        using var scope = provider.CreateScope();

        Assert.IsType<RedirectingEmailSender>(
            scope.ServiceProvider.GetRequiredService<IEmailSender>());
    }

    [Fact]
    public void A_malformed_redirect_address_stops_the_application()
    {
        using var provider = Build(
            ("Email:Enabled", "true"), ("Email:RedirectAllTo", "not-an-address"));
        using var scope = provider.CreateScope();

        var ex = Assert.Throws<OptionsValidationException>(
            () => scope.ServiceProvider.GetRequiredService<IOptions<EmailSettings>>().Value);

        Assert.Contains("must not fall through to real recipients", string.Join(" ", ex.Failures));
    }

    // Guards the file itself, because the redirect being committed is what makes a fresh
    // clone safe. Deleting that line is exactly the mistake this whole design exists to stop.
    [Fact]
    public void The_committed_development_configuration_redirects_all_mail()
    {
        var path = Path.Combine(RepositoryRoot(), "src", "AlertHub.Api", "appsettings.Development.json");
        Assert.True(File.Exists(path), $"Expected development settings at {path}");

        var configuration = new ConfigurationBuilder().AddJsonFile(path).Build();
        var redirect = configuration["Email:RedirectAllTo"];

        Assert.False(string.IsNullOrWhiteSpace(redirect),
            "appsettings.Development.json must set Email:RedirectAllTo, or a developer running "
          + "locally will email the addresses in the seed data.");

        Assert.True(
            new EmailSettingsValidator()
                .Validate(null, new EmailSettings { RedirectAllTo = redirect })
                .Succeeded,
            $"Email:RedirectAllTo is '{redirect}', which would fail startup validation.");
    }

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "AlertHub.slnx")))
            directory = directory.Parent;

        return directory?.FullName
            ?? throw new InvalidOperationException("Could not locate the repository root.");
    }
}

/// <summary>
/// This sender stands in for the email one whenever email is off, which is the default in
/// development. Its line is therefore the only place a developer can confirm who would have
/// been written to, so the addresses have to be in it.
/// </summary>
public class LoggingNotificationSenderTests
{
    private static EscalationNotification ANotification(params EscalationTargetDto[] recipients) =>
        new(Guid.NewGuid(), "Checkout returning 500s", IncidentSeverity.P1_Critical,
            Guid.NewGuid(), "Payments API", 2, recipients);

    [Fact]
    public async Task The_line_names_every_recipient_and_their_address()
    {
        var logger = new ListLogger<LoggingNotificationSender>();
        var sender = new LoggingNotificationSender(logger);

        await sender.SendAsync(
            ANotification(
                new EscalationTargetDto(Guid.NewGuid(), "Dana Responder", "dana@acme.test", EscalationTargetSource.OnCall),
                new EscalationTargetDto(Guid.NewGuid(), "Eli Responder", "eli@acme.test", EscalationTargetSource.Role)),
            CancellationToken.None);

        var line = Assert.Single(logger.Lines);
        Assert.Contains("Dana Responder <dana@acme.test> [OnCall]", line);
        Assert.Contains("Eli Responder <eli@acme.test> [Role]", line);
    }

    [Fact]
    public async Task Reaching_nobody_is_reported_as_a_warning()
    {
        var logger = new ListLogger<LoggingNotificationSender>();
        var sender = new LoggingNotificationSender(logger);

        await sender.SendAsync(ANotification(), CancellationToken.None);

        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
        Assert.Contains("reached nobody", Assert.Single(logger.Lines));
    }
}

internal sealed class ListLogger<T> : ILogger<T>
{
    public List<(LogLevel Level, string Message)> Entries { get; } = [];

    public IEnumerable<string> Lines => Entries.Select(e => e.Message);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        Entries.Add((logLevel, formatter(state, exception)));
}

public class EmailCredentialValidationTests
{
    private static bool IsValid(EmailSettings settings) =>
        new EmailSettingsValidator().Validate(null, settings).Succeeded;

    private static EmailSettings Enabled(string? username, string? password) =>
        new()
        {
            Enabled = true,
            Smtp = { Host = "smtp.example.test", Port = 587, Username = username, Password = password },
        };

    // Authenticating with an empty password is never intentional, so a half-filled
    // credential is refused at startup rather than attempted at send time.
    [Fact]
    public void A_username_without_a_password_fails_startup()
    {
        Assert.False(IsValid(Enabled("alerts@example.test", null)));
        Assert.False(IsValid(Enabled("alerts@example.test", "")));
    }

    [Fact]
    public void A_complete_credential_is_accepted()
    {
        Assert.True(IsValid(Enabled("alerts@example.test", "secret")));
    }

    // An unauthenticated relay is normal for a local catcher.
    [Fact]
    public void No_credential_at_all_is_accepted()
    {
        Assert.True(IsValid(Enabled(null, null)));
    }
}
