using Dapper;
using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Dal.Settings;
using LeetCodeBot.Enums;
using Npgsql;

namespace LeetCodeBot.Dal.Repositories;

public class UsersRepository : IUsersRepository
{
    private readonly string _connectionString;

    public UsersRepository(
        IConfiguration configuration) 
        : this(configuration.GetConnectionString("Default")) { }
    
    public UsersRepository(
        string connectionString)
    {
        _connectionString = connectionString;
    }
    
    public async Task AddUserAsync(UserEntity user)
    {
        await using var connection = new NpgsqlConnection(_connectionString);
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

    public Task<UserEntity> GetUserAsync(long userId)
    {
        throw new NotImplementedException();
    }

    public Task DeleteUserAsync(long userId)
    {
        throw new NotImplementedException();
    }

    public Task UpdateUserAsync(UserEntity user)
    {
        throw new NotImplementedException();
    }

    public Task<IAsyncEnumerable<UserEntity>> GetUsersAsync(TimeStamp timeStamp)
    {
        throw new NotImplementedException();
    }
}