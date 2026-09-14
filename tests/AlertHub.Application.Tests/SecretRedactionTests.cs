using AlertHub.Application.Auth.Login;
using AlertHub.Application.Auth.Refresh;
using AlertHub.Application.Features.Users;
using AlertHub.Domain.Entities;

namespace AlertHub.Application.Tests;

/// <summary>
/// Records synthesize a ToString() that prints every property, and these four carry
/// credentials through a message pipeline that logs failures. A failed sign-in was enough
/// to write a plaintext password into the log file.
///
/// These assert on ToString() because that is the single hook every one of those paths goes
/// through — structured logging, string interpolation, exception messages.
/// </summary>
public class SecretRedactionTests
{
    private const string Secret = "Sup3rSecretPassword";
    private const string Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.token";

    [Fact]
    public void A_login_command_does_not_print_the_password()
    {
        var rendered = new LoginCommand("dana@acme.test", Secret).ToString();

        Assert.DoesNotContain(Secret, rendered);
        // The account that failed is the reason the line exists, so it stays.
        Assert.Contains("dana@acme.test", rendered);
    }

    [Fact]
    public void A_create_user_command_does_not_print_the_password()
    {
        var rendered = new CreateUserCommand(
            "Dana Responder", Secret, "dana@acme.test",
            PermissionRole.Responder, EscalationRole.Responder).ToString();

        Assert.DoesNotContain(Secret, rendered);
        Assert.Contains("dana@acme.test", rendered);
        Assert.Contains("Responder", rendered);
    }

    [Fact]
    public void A_refresh_command_prints_neither_token()
    {
        var rendered = new RefreshCommand(Token, "refresh-token-value").ToString();

        Assert.DoesNotContain(Token, rendered);
        Assert.DoesNotContain("refresh-token-value", rendered);
    }

    [Fact]
    public void A_login_response_prints_neither_token()
    {
        var rendered = new LoginResponse(Token, "refresh-token-value").ToString();

        Assert.DoesNotContain(Token, rendered);
        Assert.DoesNotContain("refresh-token-value", rendered);
    }

    // Redaction must not cost the records their value semantics: Wolverine and the tests
    // both rely on equality, which a hand-written ToString() does not touch but a careless
    // conversion to a class would.
    [Fact]
    public void Redaction_leaves_record_equality_intact()
    {
        Assert.Equal(
            new LoginCommand("dana@acme.test", Secret),
            new LoginCommand("dana@acme.test", Secret));

        Assert.NotEqual(
            new LoginCommand("dana@acme.test", Secret),
            new LoginCommand("dana@acme.test", "different"));
    }
}
