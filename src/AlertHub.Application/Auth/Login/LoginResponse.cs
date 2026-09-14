using System.Text;

namespace AlertHub.Application.Auth.Login;

public record LoginResponse(string AccessToken, string RefreshToken)
{
    /// <summary>
    /// A response is as loggable as a command — Wolverine carries this one back through the
    /// same pipeline — and these two tokens are exactly what an attacker would want from a
    /// log file.
    /// </summary>
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(AccessToken)} = [redacted], {nameof(RefreshToken)} = [redacted]");
        return true;
    }
}
