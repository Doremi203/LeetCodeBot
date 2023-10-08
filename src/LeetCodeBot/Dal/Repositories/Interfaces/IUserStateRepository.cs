using LeetCodeBot.Enums;

namespace LeetCodeBot.Dal.Repositories.Interfaces;

public interface IUserStateRepository
{
    UserState GetState(long userId);

    bool CheckState(long userId, UserState state);
    
    void AddOrUpdateState(long userId, UserState state);
}