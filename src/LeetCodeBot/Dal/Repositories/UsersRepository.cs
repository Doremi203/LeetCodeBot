using Dapper;
using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Dal.Settings;
using LeetCodeBot.Enums;
using Microsoft.Extensions.Options;
using Npgsql;

namespace LeetCodeBot.Dal.Repositories;

public class UsersRepository : BaseRepository, IUsersRepository
{
    public UsersRepository(
        IOptionsSnapshot<DalOptions> dalOptions)
    : base(dalOptions) { }
    
    public async Task AddUserAsync(UserEntity user)
    {
        await using var connection = new NpgsqlConnection(DalSettings.Value.ConnectionString);
        await AddUserAsync(connection, user);
    }
    
    public static async Task AddUserAsync(NpgsqlConnection connection, UserEntity user)
    {
        const string sqlQuery = @"
            INSERT INTO users (telegram_user_id, difficulty, time_setting, state, is_premium)
            VALUES (@TelegramUserId, @Difficulty, @TimeSetting, @State, @IsPremium)
                ";
        
        await connection.ExecuteAsync(sqlQuery, user);
    }

    public async Task<UserEntity?> GetUserAsync(long userId)
    {
        await using var connection = new NpgsqlConnection(DalSettings.Value.ConnectionString);
        return await GetUserAsync(connection, userId);
    }
    
    public static async Task<UserEntity?> GetUserAsync(NpgsqlConnection connection, long userId)
    {
        const string sqlQuery = $@"
            SELECT telegram_user_id, difficulty, time_setting, state, is_premium
            FROM users
            WHERE telegram_user_id = @UserId
                ";
        
        var sqlQueryParams = new { UserId = userId };

        return await connection
            .QueryFirstOrDefaultAsync<UserEntity>(sqlQuery, sqlQueryParams);
    }
    
    public async Task DeleteUserAsync(long userId)
    {
        await using var connection = new NpgsqlConnection(DalSettings.Value.ConnectionString);
        await DeleteUserAsync(connection, userId);
    }

    public static async Task DeleteUserAsync(NpgsqlConnection connection, long userId)
    {
        const string sqlQuery = @"
            DELETE FROM users
            WHERE telegram_user_id = @UserId
                ";
        
        var sqlQueryParams = new { UserId = userId };
        
        await connection.ExecuteAsync(sqlQuery, sqlQueryParams);
    }

    public async Task UpdateUserAsync(UserEntity user)
    {
        await using var connection = new NpgsqlConnection(DalSettings.Value.ConnectionString);
        await UpdateUserAsync(connection, user);
    }
    
    public static async Task UpdateUserAsync(NpgsqlConnection connection, UserEntity user)
    {
        const string sqlQuery = @"
            UPDATE users
            SET difficulty = COALESCE(@Difficulty, difficulty),
                time_setting = COALESCE(@TimeSetting, time_setting),
                state = COALESCE(@State, state),
                is_premium = COALESCE(@IsPremium, is_premium)
            WHERE telegram_user_id = @TelegramUserId
                ";
        
        var sqlQueryParams = new
        {
            TelegramUserId = user.TelegramUserId,
            Difficulty = user.Difficulty,
            TimeSetting = user.TimeSetting,
            State = user.State,
            IsPremium = user.IsPremium
        };
        
        await connection.ExecuteAsync(sqlQuery, sqlQueryParams);
    }

    public async Task<IEnumerable<UserEntity>> GetUsersByTimeAsync(TimeStamp timeStamp)
    {
        await using var connection = new NpgsqlConnection(DalSettings.Value.ConnectionString);
        return await GetUsersByTimeAsync(connection, timeStamp);
    }
    
    public static async Task<IEnumerable<UserEntity>> GetUsersByTimeAsync(NpgsqlConnection connection, TimeStamp timeStamp)
    {
        const string sqlQuery = @"
            SELECT telegram_user_id, difficulty, time_setting, state, is_premium
            FROM users
            WHERE time_setting = @TimeStamp
                ";
        
        var sqlQueryParams = new { TimeStamp = timeStamp };
        
        return await connection.QueryAsync<UserEntity>(sqlQuery, sqlQueryParams);
    }
}