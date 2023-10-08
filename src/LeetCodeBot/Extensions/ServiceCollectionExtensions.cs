using LeetCodeBot.Dal.Repositories;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Dal.Settings;

namespace LeetCodeBot.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDalRepositories(
        this IServiceCollection services)
    {
        services.AddScoped<IRegisteredUsersRepository, RegisteredUsersRepository>();
        services.AddScoped<ISolvedQuestionsRepository, SolvedQuestionsRepository>();
        services.AddScoped<IUserSettingsRepository, UserSettingsRepository>();
        services.AddScoped<IUserStateRepository, UserStateRepository>();
        
        return services;
    }

    public static IServiceCollection AddDalInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<DalOptions>(configuration.GetSection(nameof(DalOptions)));
        
        return services;
    }
}