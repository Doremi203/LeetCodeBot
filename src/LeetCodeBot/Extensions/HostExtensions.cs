using FluentMigrator.Runner;

namespace LeetCodeBot.Extensions;

public static class HostExtensions
{
    public static IHost MigrationUp(this IHost app)
    {
        using var scope = app.Services.CreateScope();
        var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
        runner.MigrateUp();
        return app;
    }
}