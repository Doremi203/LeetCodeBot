using LeetCodeBot.Enums;
using Telegram.Bot.Types;

namespace LeetCodeBot.Dal.Entities;

public record UserStateEntity(
    long Id,
    UserState State
    );