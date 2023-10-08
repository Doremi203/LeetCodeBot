using System.Collections.Concurrent;
using LeetCodeBot.Dal.Repositories.Interfaces;

namespace LeetCodeBot.Dal.Repositories;

public class SolvedQuestionsRepository : ISolvedQuestionsRepository
{
    private ConcurrentDictionary<long, ConcurrentBag<int>> _solvedQuestions = new();
    
    public async Task<ConcurrentBag<int>> GetAllSolvedQuestionsByUserIdAsync(long userId)
    {
        _solvedQuestions.TryAdd(userId, new ConcurrentBag<int>());
        return await Task.FromResult(_solvedQuestions[userId]);
    }

    public Task AddSolvedQuestionAsync(long userId, int problemId)
    {
        _solvedQuestions[userId].Add(problemId);
        return Task.CompletedTask;
    }
}