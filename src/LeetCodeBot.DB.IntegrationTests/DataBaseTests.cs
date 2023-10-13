using System.Transactions;
using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories;
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
        {
            TelegramUserId = 1, 
            Difficulty = Difficulty.Easy, 
            TimeSetting = TimeStamp.Ten, 
            State = UserState.DifficultySetup, 
            IsPremium = false
        };

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
        {
            TelegramUserId = 1, 
            Difficulty = Difficulty.Easy, 
            TimeSetting = TimeStamp.Ten, 
            State = UserState.DifficultySetup, 
            IsPremium = false
        };

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
        { 
        TelegramUserId = 1,
        Difficulty = Difficulty.Easy,
        TimeSetting = TimeStamp.Ten,
        State = UserState.DifficultySetup,
        IsPremium = false
    };

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
    public async Task GetAllSolvedQuestionsByUser_When_DataIsOk_1()
    {
        var user = new UserEntity
        {
            TelegramUserId = 1,
            Difficulty = Difficulty.Easy,
            TimeSetting = TimeStamp.Ten,
            State = UserState.DifficultySetup,
            IsPremium = false
        };

        var solvedQuestion1 = new SolvedQuestionsEntity
        { 
            Date = DateTime.UtcNow,
            QuestionId = 1
        };
        
        var solvedQuestion2 = new SolvedQuestionsEntity
        { 
            Date = DateTime.UtcNow.AddHours(1),
            QuestionId = 2
        };
        
        var solvedQuestion3 = new SolvedQuestionsEntity
        { 
            Date = DateTime.UtcNow.AddHours(2),
            QuestionId = 3
        };

        using var transaction = new TransactionScope(
            TransactionScopeAsyncFlowOption.Enabled);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();

        await UsersRepository.AddUserAsync(connection, user);
        await SolvedQuestionsRepository.AddSolvedQuestionAsync(connection, user.TelegramUserId, solvedQuestion1);
        await SolvedQuestionsRepository.AddSolvedQuestionAsync(connection, user.TelegramUserId, solvedQuestion2);
        await SolvedQuestionsRepository.AddSolvedQuestionAsync(connection, user.TelegramUserId, solvedQuestion3);
        var solvedQuestionFromDb = 
            (await SolvedQuestionsRepository.GetAllSolvedQuestionsByUserIdAsync(connection, user.TelegramUserId))
            .ToArray();
        Assert.Contains(solvedQuestion1, solvedQuestionFromDb);
        Assert.Contains(solvedQuestion2, solvedQuestionFromDb);
        Assert.Contains(solvedQuestion3, solvedQuestionFromDb);
    }

    [Fact]
    public async Task DeleteUser_When_PresentInDB()
    {
        var user = new UserEntity
        { 
        TelegramUserId = 1,
        Difficulty = Difficulty.Easy,
        TimeSetting = TimeStamp.Ten,
        State = UserState.DifficultySetup,
        IsPremium = false
    };

    await using var connection = new NpgsqlConnection(ConnectionString);
        
        await UsersRepository.AddUserAsync(connection, user);
        await UsersRepository.DeleteUserAsync(connection, user.TelegramUserId);
        var userFromDb = await UsersRepository.GetUserAsync(connection, user.TelegramUserId);
        Assert.Null(userFromDb);
    }

    [Fact]
    public async Task UpdateUser_When_PresentInDB()
    {
        var user = new UserEntity
        {
            TelegramUserId = 1,
            Difficulty = Difficulty.Easy,
            TimeSetting = TimeStamp.Ten,
            State = UserState.DifficultySetup,
            IsPremium = false
        };

        var userUpdated = new UserEntity
        {
            TelegramUserId = 1,
            Difficulty = Difficulty.Hard,
            TimeSetting = TimeStamp.Ten,
            State = UserState.Registered,
            IsPremium = false
        };

    using var transaction = new TransactionScope(
            TransactionScopeAsyncFlowOption.Enabled);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        await UsersRepository.AddUserAsync(connection, user);
        await UsersRepository.UpdateUserAsync(connection, userUpdated);
        
        var userFromDb = await UsersRepository.GetUserAsync(connection, user.TelegramUserId);
        
        Assert.Equal(userUpdated, userFromDb);
    }
    
    [Fact]
    public async Task PartialUpdateUser_When_PresentInDB()
    {
        var user = new UserEntity
        {
            TelegramUserId = 1,
            Difficulty = Difficulty.Easy,
            TimeSetting = TimeStamp.Ten,
            State = UserState.DifficultySetup,
            IsPremium = false
        };

        var userUpdated = new UserEntity
        {
            TelegramUserId = 1,
            Difficulty = Difficulty.Hard,
            State = UserState.Registered,
        };

        using var transaction = new TransactionScope(
            TransactionScopeAsyncFlowOption.Enabled);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        await UsersRepository.AddUserAsync(connection, user);
        await UsersRepository.UpdateUserAsync(connection, userUpdated);
        
        var userFromDb = await UsersRepository.GetUserAsync(connection, user.TelegramUserId);
        
        var userRequired = new UserEntity
        {
            TelegramUserId = 1,
            Difficulty = Difficulty.Hard,
            TimeSetting = TimeStamp.Ten,
            State = UserState.Registered,
            IsPremium = false
        };
        
        Assert.Equal(userRequired, userFromDb);
    }

    [Fact]
    public async Task GetUsersByTimeStamp_When_PresentInDB()
    {
        var user1 = new UserEntity
        {
            TelegramUserId = 1,
            Difficulty = Difficulty.Easy,
            TimeSetting = TimeStamp.Ten,
            State = UserState.DifficultySetup,
            IsPremium = false
        };

        var user2 = new UserEntity
        {
            TelegramUserId = 2,
            Difficulty = Difficulty.Hard,
            TimeSetting = TimeStamp.Ten,
            State = UserState.Registered,
            IsPremium = false
        };

        var user3 = new UserEntity
        {
            TelegramUserId = 3,
            Difficulty = Difficulty.Easy,
            TimeSetting = TimeStamp.Fourteen,
            State = UserState.DifficultySetup,
            IsPremium = false
        };

        var user4 = new UserEntity
        {
            TelegramUserId = 4,
            Difficulty = Difficulty.Hard,
            TimeSetting = TimeStamp.Fourteen,
            State = UserState.Registered,
            IsPremium = false
        };

    using var transaction = new TransactionScope(
            TransactionScopeAsyncFlowOption.Enabled);
        await using var connection = new NpgsqlConnection(ConnectionString);
        await connection.OpenAsync();
        
        await UsersRepository.AddUserAsync(connection, user1);
        await UsersRepository.AddUserAsync(connection, user2);
        await UsersRepository.AddUserAsync(connection, user3);
        await UsersRepository.AddUserAsync(connection, user4);
        
        var usersFromDb = (await UsersRepository.GetUsersByTimeAsync(connection, TimeStamp.Ten)).ToArray();
        
        Assert.Contains(user1, usersFromDb);
        Assert.Contains(user2, usersFromDb);
        Assert.DoesNotContain(user3, usersFromDb);
        Assert.DoesNotContain(user4, usersFromDb);
        
        usersFromDb = (await UsersRepository.GetUsersByTimeAsync(connection, TimeStamp.Fourteen)).ToArray();
        
        Assert.DoesNotContain(user1, usersFromDb);
        Assert.DoesNotContain(user2, usersFromDb);
        Assert.Contains(user3, usersFromDb);
        Assert.Contains(user4, usersFromDb);
    }
}