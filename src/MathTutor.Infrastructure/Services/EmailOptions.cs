namespace MathTutor.Infrastructure.Services;

public sealed class EmailOptions
{
    public bool Enabled { get; set; }
    public bool UseSmtp { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public bool UseSsl { get; set; } = true;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FromAddress { get; set; } = "noreply@mathtutor.local";
    public string FromName { get; set; } = "MathTutor";
}
