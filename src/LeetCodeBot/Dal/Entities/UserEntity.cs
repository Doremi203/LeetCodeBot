using LeetCodeBot.Enums;

namespace LeetCodeBot.Dal.Entities;

public record UserEntity(
    long TelegramUserId,
    UserState State,
    Difficulty Difficulty,
    TimeStamp Time,
    bool? IsPremium
    );