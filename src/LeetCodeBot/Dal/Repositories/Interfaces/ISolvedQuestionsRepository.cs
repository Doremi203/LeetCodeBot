using System.Collections.Concurrent;
using LeetCodeBot.Dal.Entities;

namespace LeetCodeBot.Dal.Repositories.Interfaces;

public interface ISolvedQuestionsRepository
{
    Task<ConcurrentBag<int>> GetAllSolvedQuestionsByUserIdAsync(long userId);
    Task AddSolvedQuestionAsync(long userId, int problemId);
}