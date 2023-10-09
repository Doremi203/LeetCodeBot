using System.Transactions;
using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories;
using LeetCodeBot.Dal.Settings;
using LeetCodeBot.Enums;
using Npgsql;

namespace LeetCodeBot.DB.IntegrationTests;

public class DataBaseTests
{
    private const string ConnectionString = "User ID=akari;Password=rtyufghj;Host=localhost;Port=15432;Database=leetcode-bot;Pooling=true;";

    public DataBaseTests()
    {
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
    }

    [Fact]
    public async Task AddUser_When_DataIsOk_1()
    {
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        var user = new UserEntity
        (
            TelegramUserId: 1, 
            Difficulty: Difficulty.Easy, 
            TimeSetting: TimeStamp.Ten, 
            State: UserState.DifficultySetup, 
            IsPremium: false
        );

        var transaction = await connection.BeginTransactionAsync();
        try
        {
            await UsersRepository.AddUserAsync(connection, user);
            var userFromDb = await UsersRepository.GetUserAsync(connection, 1);
        
            Assert.Equal(user, userFromDb);
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }
    
    [Fact]
    public async Task AddUser_When_DataIsOk_2()
    {
        var user = new UserEntity
        (
            TelegramUserId: 1, 
            Difficulty: Difficulty.Easy, 
            TimeSetting: TimeStamp.Ten, 
            State: UserState.DifficultySetup, 
            IsPremium: false
        );

        using var transaction = new TransactionScope(
            TransactionScopeAsyncFlowOption.Enabled);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await UsersRepository.AddUserAsync(connection, user);
        var userFromDb = await UsersRepository.GetUserAsync(connection, 1);

        Assert.Equal(user, userFromDb);
    }

    [Fact]
    public async Task AddSolvedQuestion_When_DataIsOk_1()
    {
        var user = new UserEntity
        (
            TelegramUserId: 1,
            Difficulty: Difficulty.Easy,
            TimeSetting: TimeStamp.Ten,
            State: UserState.DifficultySetup,
            IsPremium: false
        );

        var solvedQuestion = new SolvedQuestionsEntity
        { 
        Date = DateTime.UtcNow,
        QuestionId = 1
        };

    using var transaction = new TransactionScope(
            TransactionScopeAsyncFlowOption.Enabled);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await UsersRepository.AddUserAsync(connection, user);
        await SolvedQuestionsRepository.AddSolvedQuestionAsync(connection, user.TelegramUserId, solvedQuestion);
        var solvedQuestionFromDb = 
            await SolvedQuestionsRepository.GetAllSolvedQuestionsByUserIdAsync(connection, user.TelegramUserId);
        Assert.Contains(solvedQuestion, solvedQuestionFromDb);
    }
    
    [Fact]
    public async Task GetUser_When_DataIsOk()
    {
        var dalOptions = new DalOptions{ConnectionString = ConnectionString};
        var userRep = new UsersRepository(dalOptions);
        var user = new UserEntity
        (
            TelegramUserId: 1, 
            Difficulty: Difficulty.Easy, 
            TimeSetting: TimeStamp.Ten, 
            State: UserState.DifficultySetup, 
            IsPremium: false
        );
        await userRep.AddUserAsync(user);
    }
}