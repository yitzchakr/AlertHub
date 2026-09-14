using System.Text;

namespace AlertHub.Application.Auth.Login;

public record LoginCommand(string Email, string Password)
{
    /// <summary>
    /// A record synthesizes a ToString() that prints every property, so any log line
    /// holding this command — Wolverine's failure logging, a structured log carrying the
    /// message, an exception that interpolates it — would write the plaintext password to
    /// the log file. PrintMembers is the hook the compiler routes ToString() through, so
    /// overriding it covers every one of those paths at once.
    ///
    /// The email stays: knowing which account failed is the point of the log line.
    /// </summary>
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(Email)} = {Email}, {nameof(Password)} = [redacted]");
        return true;
    }
}
