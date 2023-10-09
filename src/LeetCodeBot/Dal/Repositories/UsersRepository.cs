using Dapper;
using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Dal.Settings;
using LeetCodeBot.Enums;
using LeetCodeBot.Exceptions;
using Npgsql;

namespace LeetCodeBot.Dal.Repositories;

public class UsersRepository : BaseRepository, IUsersRepository
{
    public UsersRepository(
        DalOptions dalOptions)
    : base(dalOptions) { }
    
    public async Task AddUserAsync(UserEntity user)
    {
        await using var connection = new NpgsqlConnection(DalSettings.ConnectionString);
        await AddUserAsync(connection, user);
    }
    
    public static async Task AddUserAsync(NpgsqlConnection connection, UserEntity user)
    {
        const string sqlQuery = @"
            INSERT INTO users (telegram_user_id, difficulty, time_setting, state, is_premium)
            VALUES (@TelegramUserId, @Difficulty, @TimeSetting, @State, @IsPremium)
                ";
        
        await connection.QueryAsync(sqlQuery, user);
    }

    public async Task<UserEntity> GetUserAsync(long userId)
    {
        await using var connection = new NpgsqlConnection(DalSettings.ConnectionString);
        return await GetUserAsync(connection, userId);
    }
    
    public static async Task<UserEntity> GetUserAsync(NpgsqlConnection connection, long userId)
    {
        const string sqlQuery = $@"
            SELECT telegram_user_id, difficulty, time_setting, state, is_premium
            FROM users
            WHERE telegram_user_id = @UserId
                ";
        
        var sqlQueryParams = new { UserId = userId };
        
        return await connection
            .QueryFirstOrDefaultAsync<UserEntity>(sqlQuery, sqlQueryParams) 
               ?? throw new UserNotFoundException($"User {userId} not found");
    }

    public Task DeleteUserAsync(long userId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateUserAsync(UserEntity user)
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<UserEntity>> GetUsersAsync(TimeStamp timeStamp)
    {
        throw new NotImplementedException();
    }
}