using LeetCodeBot.Enums;

namespace LeetCodeBot.Dal.Entities;

public record UserSettingsEntity(
    long UserId,
    Difficulty Difficulty,
    TimeStamp Time,
    bool? IsPremium
    );