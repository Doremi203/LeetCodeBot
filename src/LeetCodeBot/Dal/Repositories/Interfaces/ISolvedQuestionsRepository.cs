using System.Collections.Concurrent;
using LeetCodeBot.Dal.Entities;

namespace LeetCodeBot.Dal.Repositories.Interfaces;

public interface ISolvedQuestionsRepository
{
    Task<IEnumerable<SolvedQuestionsEntity>> GetAllSolvedQuestionsByUserIdAsync(long userId);
    Task AddSolvedQuestionAsync(long userId, SolvedQuestionsEntity solvedQuestion);
}