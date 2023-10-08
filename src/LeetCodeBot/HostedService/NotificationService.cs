using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Enums;
using LeetCodeBot.Models;
using LeetCodeBot.Services.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace LeetCodeBot.HostedService;

public class NotificationService : BackgroundService
{
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly ISolvedQuestionsRepository _solvedQuestionsRepository;
    private readonly ITelegramBotClient _telegramBotClient;
    private readonly IUserStateRepository _userStateRepository;
    private readonly IServiceProvider _serviceProvider;

    public NotificationService(
        IUserSettingsRepository userSettingsRepository,
        ISolvedQuestionsRepository solvedQuestionsRepository,
        ITelegramBotClient telegramBotClient,
        IUserStateRepository userStateRepository,
        IServiceProvider serviceProvider
        )
    {
        _userSettingsRepository = userSettingsRepository;
        _solvedQuestionsRepository = solvedQuestionsRepository;
        _telegramBotClient = telegramBotClient;
        _userStateRepository = userStateRepository;
        _serviceProvider = serviceProvider;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var users = _userSettingsRepository.GetAll();
            foreach (var user in users)
            {
                var time = user.Time;
                var moscowTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow");
                var now = TimeZoneInfo.ConvertTimeToUtc(DateTime.Now);
                now = TimeZoneInfo.ConvertTimeFromUtc(now, moscowTimeZone);

                if (user.Difficulty == Difficulty.NotSet)
                    continue;
                if (time == TimeStamp.NotSet)
                    continue;

                if (now.Hour is >= 0 and <= 2)
                {
                    _userStateRepository.AddOrUpdateState(user.UserId, UserState.ToNotificate);
                }
                
                if (!_userStateRepository.CheckState(user.UserId, UserState.ToNotificate))
                    continue;
                
                switch (time)
                {
                    case TimeStamp.NotSet:
                        continue;
                    case TimeStamp.Ten when now.Hour is >= 10 and < 11:
                    case TimeStamp.Fourteen when now.Hour is >= 14 and < 15:
                    case TimeStamp.Sixteen when now.Hour is >= 18 and < 19:
                    case TimeStamp.TwentyTwo when now.Hour is >= 22 and < 23:
                        await SendNotification(user.UserId, user.Difficulty, stoppingToken);
                        break;
                    default:
                        continue;
                }
            }

            if (users.Any())
                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            else
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task<Message> SendNotification(long userId, Difficulty difficulty, CancellationToken stoppingToken)
    {
        LeetcodeQuestionType? question;
        using (var scope = _serviceProvider.CreateScope())
        {
            var leetcodeQuestionService = scope.ServiceProvider.GetRequiredService<IGetLeetcodeQuestionService>();
            var questions = await leetcodeQuestionService.GetLeetcodeQuestionsAsync(difficulty);
            var solvedQuestions = await _solvedQuestionsRepository.GetAllSolvedQuestionsByUserIdAsync(userId);
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
        
        _userStateRepository.AddOrUpdateState(userId, UserState.Notificated);
        
        InlineKeyboardMarkup inlineKeyboard = new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Solved", $"ProblemSolved {question.FrontendQuestionId}"),
                },
            });
        
        return await _telegramBotClient.SendTextMessageAsync(
            chatId: userId,
            text: replyMessage,
            replyMarkup: inlineKeyboard,
            cancellationToken: stoppingToken);
    }
}