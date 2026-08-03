namespace backend.Dtos;

public record AppSettingDto(string Key, string Value, DateTimeOffset UpdatedAt);

public record UpdateAppSettingRequest(string Value);
