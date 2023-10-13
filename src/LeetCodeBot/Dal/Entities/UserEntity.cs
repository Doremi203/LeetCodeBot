using LeetCodeBot.Enums;

namespace LeetCodeBot.Dal.Entities;

public record UserEntity
{
    public long TelegramUserId { get; init; }
    public Difficulty? Difficulty { get; init; }
    public TimeStamp? TimeSetting { get; init; }
    public UserState? State { get; init; }
    public bool? IsPremium { get; init; }
    
    public static UserEntity Empty => new();
}