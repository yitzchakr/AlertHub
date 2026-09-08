using Microsoft.Extensions.Options;
using System.Text;

namespace AlertHub.Application.Settings;

/// <summary>
/// Runs at startup rather than at first sign-in. The signing key is deliberately absent from
/// appsettings.json so it never reaches source control, which means a fresh clone has no key
/// at all. Without this, that omission surfaces as an ArgumentNullException raised from inside
/// token creation on the first authenticated request — a stack trace that names neither the
/// setting nor the fix. Refusing to boot instead turns an afternoon into five seconds.
///
/// RefreshTokenExpirationDays is not checked because nothing reads it: both LoginHandler and
/// RefreshHandler hardcode seven days. Validating a setting that has no effect would promise
/// the reader a control that does not exist.
/// </summary>
public class JwtSettingsValidator : IValidateOptions<JwtSettings>
{
    /// <summary>
    /// HS256 signs with SHA-256, and the handler refuses a key shorter than that hash rather
    /// than quietly padding it, so a short key fails at first use instead of at startup.
    /// </summary>
    private const int MinimumKeyBytes = 32;

    public ValidateOptionsResult Validate(string? name, JwtSettings options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            failures.Add(
                "Jwt:SecretKey is not configured. It is kept out of appsettings.json on purpose. "
              + "In development run: dotnet user-secrets set \"Jwt:SecretKey\" \"<key>\" "
              + "--project src/AlertHub.Api. Elsewhere set the Jwt__SecretKey environment "
              + "variable. Generate a key with: openssl rand -base64 64");
        }
        else if (Encoding.UTF8.GetByteCount(options.SecretKey) is var bytes and < MinimumKeyBytes)
        {
            failures.Add(
                $"Jwt:SecretKey is {bytes} bytes, but HS256 requires at least {MinimumKeyBytes}. "
              + "A key this short is rejected when the first token is signed, not now.");
        }

        // Both are validated on every incoming token, so a blank one rejects every request
        // with an indistinguishable 401.
        if (string.IsNullOrWhiteSpace(options.Issuer))
            failures.Add("Jwt:Issuer is required — every incoming token is validated against it.");

        if (string.IsNullOrWhiteSpace(options.Audience))
            failures.Add("Jwt:Audience is required — every incoming token is validated against it.");

        if (options.AccessTokenExpirationMinutes <= 0)
        {
            failures.Add(
                $"Jwt:AccessTokenExpirationMinutes is {options.AccessTokenExpirationMinutes}. "
              + "A non-positive lifetime issues tokens that expired before they were returned.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
