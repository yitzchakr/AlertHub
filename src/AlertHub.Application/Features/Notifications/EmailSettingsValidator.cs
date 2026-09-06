using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;

namespace AlertHub.Application.Features.Notifications;

/// <summary>
/// Runs at startup rather than at send time. A mistyped redirect address is the one
/// misconfiguration that must never be discovered late: the failure mode is real people
/// receiving test pages, so the application refuses to boot instead.
/// </summary>
public class EmailSettingsValidator : IValidateOptions<EmailSettings>
{
    public ValidateOptionsResult Validate(string? name, EmailSettings options)
    {
        var failures = new List<string>();

        if (options.RedirectAllTo is { Length: > 0 } redirect && !IsEmailAddress(redirect))
        {
            failures.Add(
                $"Email:RedirectAllTo is '{redirect}', which is not a valid address. Fix it or "
              + "remove it — a broken redirect must not fall through to real recipients.");
        }

        // Only worth enforcing once something can actually send.
        if (options.Enabled)
        {
            if (!IsEmailAddress(options.From))
                failures.Add($"Email:From is '{options.From}', which is not a valid address.");

            if (string.IsNullOrWhiteSpace(options.Smtp.Host))
                failures.Add("Email:Smtp:Host is required when Email:Enabled is true.");

            if (options.Smtp.Port is < 1 or > 65535)
                failures.Add($"Email:Smtp:Port is {options.Smtp.Port}, which is not a valid port.");

            // Authenticating with an empty password is not a thing anyone means to do, so a
            // half-filled credential is refused rather than attempted.
            if (!string.IsNullOrWhiteSpace(options.Smtp.Username)
                && string.IsNullOrEmpty(options.Smtp.Password))
            {
                failures.Add(
                    "Email:Smtp:Password is required when a username is set. Supply it through "
                  + "user-secrets or the Email__Smtp__Password environment variable.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static bool IsEmailAddress(string value) => new EmailAddressAttribute().IsValid(value);
}
