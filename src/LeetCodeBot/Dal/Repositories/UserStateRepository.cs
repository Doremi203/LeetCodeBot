using System.Collections.Concurrent;
using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Enums;

namespace LeetCodeBot.Dal.Repositories;

public class UserStateRepository : IUserStateRepository
{
    private ConcurrentDictionary<long, UserStateEntity> _userStateEntities = new();


    public UserState GetState(long userId)
    {
        if (!_userStateEntities.ContainsKey(userId))
            return UserState.NewUser;
        
        return _userStateEntities[userId].State;
    }

    public bool CheckState(long userId, UserState state) 
        => GetState(userId) == state;

    public void AddOrUpdateState(long userId, UserState state)
    {
        _userStateEntities.Remove(userId, out _);
        _userStateEntities.TryAdd(userId, new UserStateEntity(userId, state));
    }
}