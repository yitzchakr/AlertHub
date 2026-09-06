namespace AlertHub.Application.Features.Notifications;

public class EmailSettings
{
    /// <summary>
    /// Selects the email notification sender over the logging one. Off by default, so
    /// nothing sends until it is deliberately turned on.
    /// </summary>
    public bool Enabled { get; set; }

    public string From { get; set; } = "alerts@alerthub.local";

    public string FromName { get; set; } = "AlertHub";

    /// <summary>
    /// When set, every email goes here instead of its real recipient. Being set is what
    /// enables it — there is deliberately no separate on/off flag, because an
    /// enabled-but-blank pair is a state that silently mails real people.
    /// </summary>
    public string? RedirectAllTo { get; set; }

    public SmtpSettings Smtp { get; set; } = new();

    public class SmtpSettings
    {
        public string Host { get; set; } = "localhost";

        /// <summary>1025 is the default for local catchers such as smtp4dev and MailHog.</summary>
        public int Port { get; set; } = 1025;

        public bool UseStartTls { get; set; }

        public string? Username { get; set; }

        /// <summary>
        /// Supply through user-secrets or the Email__Smtp__Password environment variable.
        /// A real password does not belong in appsettings.json.
        /// </summary>
        public string? Password { get; set; }
    }
}
