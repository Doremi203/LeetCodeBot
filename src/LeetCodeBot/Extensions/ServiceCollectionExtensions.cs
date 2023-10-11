using LeetCodeBot.Dal.Repositories;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Dal.Settings;

namespace LeetCodeBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDalRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<ISolvedQuestionsRepository, SolvedQuestionsRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        
        return services;
    }

    public static IServiceCollection AddDalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DalOptions>(configuration.GetSection(nameof(DalOptions)));
        
        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        
        return services;
    }
}