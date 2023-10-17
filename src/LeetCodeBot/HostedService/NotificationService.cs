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
    
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
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
                var leetcodeQuestionService = scope.ServiceProvider.GetRequiredService<IGetLeetcodeQuestionService>();
                var questions = await leetcodeQuestionService.GetLeetcodeQuestionsAsync();
                
                foreach (var user in users)
                {
                    if (user.Difficulty is null or Difficulty.NotSet || user.State != UserState.Registered)
                        continue;

                    await SendNotification(user.TelegramUserId, questions.Where(type => type.Difficulty == user.Difficulty.Value).ToArray(), cancellationToken);
                }
            }

            await Task.Delay(TimeSpan.FromHours(1), cancellationToken);
        }
    }

    private async Task SendNotification(long userId, ICollection<LeetcodeQuestionType> questions, CancellationToken cancellationToken)
    {
        LeetcodeQuestionType? question;
        using (var scope = _serviceProvider.CreateScope())
        {
            var solvedQuestionsRepository = scope.ServiceProvider.GetRequiredService<ISolvedQuestionsRepository>();
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
        
        _logger.LogInformation($"MessageSent:{replyMessage} \n To user with UserId: {userId} at {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"))}");
        
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
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}