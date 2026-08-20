namespace UsuarioApp.Domain.Settings;

public class RecoverySettings
{
    public int LinkExpirationMinutes { get; set; } = 30;
    public int SmsCodeExpirationMinutes { get; set; } = 10;
    public int MaxCodeAttempts { get; set; } = 5;
    public int RequestCooldownSeconds { get; set; } = 60;
}
