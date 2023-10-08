using System.Collections;
using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Enums;
using LeetCodeBot.Models;

namespace LeetCodeBot.Dal.Repositories.Interfaces;

public interface IUserSettingsRepository
{
    Task AddUserAsync(long userId);
    
    Task SetTimeAsync(long userId, TimeStamp time);
    
    Task AddDifficultyAsync(long userId, Difficulty difficulty);
    
    Task<UserSettingsEntity> GetAsync(long userId);
    Task RemoveDifficultyAsync(long userId, Difficulty difficulty);
    ICollection<UserSettingsEntity> GetAll();
}