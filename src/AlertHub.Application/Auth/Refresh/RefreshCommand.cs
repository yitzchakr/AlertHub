using System.Text;

namespace AlertHub.Application.Auth.Refresh;

public record RefreshCommand(string AccessToken, string RefreshToken)
{
    /// <summary>
    /// Both fields are bearer credentials: either one in a log is enough to impersonate the
    /// account until it expires. See LoginCommand for why the record's own ToString() is
    /// the thing being replaced here.
    /// </summary>
    protected virtual bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"{nameof(AccessToken)} = [redacted], {nameof(RefreshToken)} = [redacted]");
        return true;
    }
}
