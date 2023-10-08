using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Enums;

namespace LeetCodeBot.Dal.Repositories.Interfaces;

public interface IUsersRepository
{
    Task AddUserAsync(UserEntity user);
    Task<UserEntity> GetUserAsync(long userId);
    Task DeleteUserAsync(long userId);
    Task UpdateUserAsync(UserEntity user);
    Task<IAsyncEnumerable<UserEntity>> GetUsersAsync(TimeStamp timeStamp);
}