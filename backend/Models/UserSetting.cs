namespace backend.Models;

public class UserSetting
{
    public long UserId { get; set; }

    public string SettingKey { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }

    public User User { get; set; } = null!;
}
