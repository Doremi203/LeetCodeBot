using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Enums;
using LeetCodeBot.Models;
using LeetCodeBot.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace LeetCodeBot.HostedService;

public class NotificationService : BackgroundService
{
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        ITelegramBotClient telegramBotClient,
        IServiceProvider serviceProvider,
        ILogger<NotificationService> logger)
    {
        _telegramBotClient = telegramBotClient;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
            var moscowTime = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);
            moscowTime = TimeZoneInfo.ConvertTimeFromUtc(moscowTime, moscowTimeZone);
            var time = moscowTime.Hour switch
            {
                >= 10 and < 11 => TimeStamp.Ten,
                >= 14 and < 15 => TimeStamp.Fourteen,
                >= 18 and < 19 => TimeStamp.Sixteen,
                >= 22 and < 23 => TimeStamp.TwentyTwo,
                _ => TimeStamp.NotSet
            };

            if (time != TimeStamp.NotSet)
            {
                using var scope = _serviceProvider.CreateScope();
                var usersRepository = scope.ServiceProvider.GetRequiredService<IUsersRepository>();
                var users = (await usersRepository.GetUsersByTimeAsync(time)).ToArray();

                foreach (var user in users)
                {
                    if (user.Difficulty is null or Difficulty.NotSet)
                        continue;

                    await SendNotification(user.TelegramUserId, user.Difficulty.Value, stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(55), stoppingToken);
        }
    }

    private async Task SendNotification(long userId, Difficulty difficulty, CancellationToken stoppingToken)
    {
        LeetcodeQuestionType? question;
        using (var scope = _serviceProvider.CreateScope())
        {
            var leetcodeQuestionService = scope.ServiceProvider.GetRequiredService<IGetLeetcodeQuestionService>();
            var solvedQuestionsRepository = scope.ServiceProvider.GetRequiredService<ISolvedQuestionsRepository>();
            var questions = await leetcodeQuestionService.GetLeetcodeQuestionsAsync(difficulty);
            var solvedQuestions = (await solvedQuestionsRepository.GetAllSolvedQuestionsByUserIdAsync(userId))
                .Select(entity => entity.QuestionId);
            var availableQuestions = questions
                .Where(q 
                    => !solvedQuestions
                        .Contains(q.FrontendQuestionId))
                .ToArray();
            question = availableQuestions[new Random().Next(0, availableQuestions.Length)];
        }
        
        var replyMessage = $"Time to solve some problems!\n" +
                           $"Difficulty: {question.Difficulty}\n" +
                           $"Title: {question.Title}\n" +
                           $"Id: {question.FrontendQuestionId}\n" +
                           $"Link: https://leetcode.com/problems/{question.TitleSlug}";
        
        _logger.LogInformation($"MessageSent:{replyMessage} \n To user with UserId: {userId}");
        
        InlineKeyboardMarkup inlineKeyboard = new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Solved", $"ProblemSolved {question.FrontendQuestionId}"),
                },
            });

        await _telegramBotClient.SendTextMessageAsync(
            chatId: userId,
            text: replyMessage,
            replyMarkup: inlineKeyboard,
            cancellationToken: stoppingToken).ConfigureAwait(false);
    }
}