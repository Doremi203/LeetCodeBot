using LeetCodeBot.Enums;

namespace LeetCodeBot.Dal.Entities;

public record UserEntity(
    long TelegramUserId,
    Difficulty Difficulty,
    TimeStamp TimeSetting,
    UserState State,
    bool? IsPremium);