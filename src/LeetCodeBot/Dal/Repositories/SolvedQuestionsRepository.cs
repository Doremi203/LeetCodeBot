using Dapper;
using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Dal.Settings;
using Npgsql;

namespace LeetCodeBot.Dal.Repositories;

public class SolvedQuestionsRepository : BaseRepository, ISolvedQuestionsRepository
{
    public SolvedQuestionsRepository(
        DalOptions dalSettings) 
        : base(dalSettings) { }

    public async Task<IEnumerable<SolvedQuestionsEntity>> GetAllSolvedQuestionsByUserIdAsync(long userId)
    {
        await using var connection = new NpgsqlConnection(DalSettings.ConnectionString);
        return await GetAllSolvedQuestionsByUserIdAsync(connection, userId);
    }
    
    public static async Task<IEnumerable<SolvedQuestionsEntity>> GetAllSolvedQuestionsByUserIdAsync(NpgsqlConnection connection, long userId)
    {
        const string sqlQuery = @"
            SELECT id, telegram_user_id, date, question_id
            FROM solved_questions
            WHERE telegram_user_id = @UserId
                ";
        
        var sqlQueryParams = new { UserId = userId };
        
        return await connection.QueryAsync<SolvedQuestionsEntity>(sqlQuery, sqlQueryParams);
    }

    public async Task AddSolvedQuestionAsync(long userId, SolvedQuestionsEntity solvedQuestion)
    {
        await using var connection = new NpgsqlConnection(DalSettings.ConnectionString);
        await AddSolvedQuestionAsync(connection, userId, solvedQuestion);
    }
    
    public static async Task AddSolvedQuestionAsync(NpgsqlConnection connection, long userId, SolvedQuestionsEntity solvedQuestion)
    {
        const string sqlQuery = @"
        INSERT INTO solved_questions (id, telegram_user_id, date, question_id)
            VALUES (@Id, @TelegramUserId, @Date, @QuestionId)
                ";
        
        var sqlQueryParams = new
        {
            Id = solvedQuestion.Id, 
            TelegramUserId = userId, 
            Date = solvedQuestion.Date, 
            QuestionId = solvedQuestion.QuestionId
        };
        
        await connection.QueryAsync(sqlQuery, sqlQueryParams);
    }
}