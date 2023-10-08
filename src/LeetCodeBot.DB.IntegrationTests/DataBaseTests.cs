using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories;
using LeetCodeBot.Enums;

namespace LeetCodeBot.DB.IntegrationTests;

public class DataBaseTests
{
    const string ConnectionString = "User ID=akari;Password=rtyufghj;Host=localhost;Port=15432;Database=leetcode-bot;Pooling=true;";
    [Fact]
    public async Task AddUser_When_DataIsOk()
    {
        var userRep = new UsersRepository(ConnectionString);
        var user = new UserEntity(1, UserState.ToNotificate, Difficulty.Easy, TimeStamp.Ten, false);
        await userRep.AddUserAsync(user);
    }
}