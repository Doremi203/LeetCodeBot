using FluentMigrator.Runner;
using LeetCodeBot.Dal.Repositories;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Dal.Settings;
using Microsoft.Extensions.Options;

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
        
        AddMigrations(services);
        
        return services;
    }
    
    private static void AddMigrations(IServiceCollection services)
    {
        services.AddFluentMigratorCore()
            .ConfigureRunner(rb => rb.AddPostgres()
                .WithGlobalConnectionString(s =>
                {
                    var cfg = s.GetRequiredService<IOptions<DalOptions>>();
                    return cfg.Value.ConnectionString;
                })
                .ScanIn(typeof(ServiceCollectionExtensions).Assembly).For.Migrations()
            )
            .AddLogging(lb => lb.AddFluentMigratorConsole());
    }
}