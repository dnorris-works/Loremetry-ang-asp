namespace backend.Models;

public class AppSetting
{
    public string SettingKey { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public DateTimeOffset UpdatedAt { get; set; }
}
