using System.Collections.Concurrent;
using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Enums;

namespace LeetCodeBot.Dal.Repositories;

public class UserSettingsRepository : IUserSettingsRepository
{
    private ConcurrentDictionary<long, UserSettingsEntity> _userSettings = new();

    public Task AddUserAsync(long userId)
    {
        _userSettings.TryAdd(userId, new UserSettingsEntity(userId, Difficulty.NotSet, TimeStamp.NotSet, null));
        return Task.CompletedTask;
    }

    public Task SetTimeAsync(long userId, TimeStamp time)
    {
        _userSettings[userId] = _userSettings[userId] with {Time = time};
        return Task.CompletedTask;
    }

    public Task AddDifficultyAsync(long userId, Difficulty difficulty)
    {
        var difficultyPrev = _userSettings[userId].Difficulty;
        _userSettings[userId] = _userSettings[userId] with {Difficulty = difficultyPrev | difficulty};
        return Task.CompletedTask;
    }

    public async Task<UserSettingsEntity> GetAsync(long userId)
    {
        return await Task.FromResult(_userSettings[userId]);
    }

    public Task RemoveDifficultyAsync(long userId, Difficulty difficulty)
    {
        var difficultyPrev = _userSettings[userId].Difficulty;
        _userSettings[userId] = _userSettings[userId] with {Difficulty = difficultyPrev & ~difficulty};
        return Task.CompletedTask;
    }

    public ICollection<UserSettingsEntity> GetAll()
    {
        return _userSettings.Select(pair => pair.Value).ToArray();
    }
}