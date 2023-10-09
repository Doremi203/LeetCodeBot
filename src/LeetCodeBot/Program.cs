using LeetCodeBot;
using LeetCodeBot.Extensions;
using LeetCodeBot.HostedService;
using LeetCodeBot.Services;
using LeetCodeBot.Services.Interfaces;
using Telegram.Bot;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((builderContext, services) =>
    {
        // Register Bot configuration
        services.Configure<BotConfiguration>(
            builderContext.Configuration.GetSection(BotConfiguration.Configuration));

       services.AddHttpClient("telegram_bot_client")
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    BotConfiguration? botConfig = sp.GetConfiguration<BotConfiguration>();
                    TelegramBotClientOptions options = new(botConfig.BotToken);
                    return new TelegramBotClient(options, httpClient);
                });

        services.AddScoped<UpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddScoped<IGetLeetcodeQuestionService, GetLeetcodeQuestionService>();
        services
            .AddDalRepositories()
            .AddDalInfrastructure(builderContext.Configuration);
        services.AddHostedService<PollingService>();
        services.AddHostedService<NotificationService>();
    })
    .Build();

await host.RunAsync();

#pragma warning disable CA1050 // Declare types in namespaces
#pragma warning disable RCS1110 // Declare type inside namespace.
namespace LeetCodeBot
{
    public class BotConfiguration
#pragma warning restore RCS1110 // Declare type inside namespace.
#pragma warning restore CA1050 // Declare types in namespaces
    {
        public static readonly string Configuration = "BotConfiguration";

        public string BotToken { get; set; } = "";
    }
}
