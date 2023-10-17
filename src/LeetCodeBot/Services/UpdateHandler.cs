using LeetCodeBot.Dal.Entities;
using LeetCodeBot.Dal.Repositories.Interfaces;
using LeetCodeBot.Enums;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;
using Telegram.Bot.Types.ReplyMarkups;

namespace LeetCodeBot.Services;

public class UpdateHandler : IUpdateHandler
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly ISolvedQuestionsRepository _solvedQuestionsRepository;
    private readonly IUsersRepository _usersRepository;

    private ReplyKeyboardMarkup _menuKeyboardMarkup = new(
        new[]
        {
            new KeyboardButton[] { "Get settings" },
            new KeyboardButton[] { "Change notification time" },
            new KeyboardButton[] { "Change difficulty" },
            new KeyboardButton[] { "Unsubscribe from the bot" },
        })
    {
        ResizeKeyboard = true
    };

    public UpdateHandler(
        ITelegramBotClient botClient,
        ILogger<UpdateHandler> logger,
        ISolvedQuestionsRepository solvedQuestionsRepository,
        IUsersRepository usersRepository
    )
    {
        _botClient = botClient;
        _logger = logger;
        _solvedQuestionsRepository = solvedQuestionsRepository;
        _usersRepository = usersRepository;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken cancellationToken)
    {
        var handler = update switch
        {
            // UpdateType.Unknown:
            // UpdateType.ChannelPost:
            // UpdateType.EditedChannelPost:
            // UpdateType.ShippingQuery:
            // UpdateType.PreCheckoutQuery:
            // UpdateType.Poll:
            { Message: { } message } => BotOnMessageReceived(message, cancellationToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, cancellationToken),
            { CallbackQuery: { } callbackQuery } => BotOnCallbackQueryReceived(callbackQuery, cancellationToken),
            { InlineQuery: { } inlineQuery } => BotOnInlineQueryReceived(inlineQuery, cancellationToken),
            { ChosenInlineResult: { } chosenInlineResult } => BotOnChosenInlineResultReceived(chosenInlineResult,
                cancellationToken),
            _ => UnknownUpdateHandlerAsync(update, cancellationToken)
        };

        await handler;
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Receive message type: {message.Type} from: Userid = {message.From?.Id}, " +
                               $"Username = {message.From?.Username}, " +
                               $"Time = {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"))}.");
        if (message.Text is not { } messageText)
            return; 
        
        var userId = message.From!.Id;
        Task<Message>? action = null;
        var curUser = await _usersRepository.GetUserAsync(userId);
        if (curUser is null)
        {
            action = Start();
        }
        else
        {
            switch (curUser.State)
            {
                case UserState.TimeSetup:
                    action = messageText switch
                    {
                        "10:00" => SetupTime(TimeStamp.Ten),
                        "14:00" => SetupTime(TimeStamp.Fourteen),
                        "18:00" => SetupTime(TimeStamp.Sixteen),
                        "22:00" => SetupTime(TimeStamp.TwentyTwo),
                        _ => null
                    };
                    break;
                case UserState.DifficultySetup:
                    action = messageText switch
                    {
                        "Easy" => AddDifficulty(userId, Difficulty.Easy, cancellationToken),
                        "Medium" => AddDifficulty(userId, Difficulty.Medium, cancellationToken),
                        "Hard" => AddDifficulty(userId, Difficulty.Hard, cancellationToken),
                        "Save difficulty" => SaveDifficulty(),
                        _ => null
                    };
                    break;
                case UserState.Registered:
                    switch (messageText)
                    {
                        case "Get settings":
                            action = GetUserSettings();
                            break;
                        case "Change notification time":
                            action = SendTimeReplyKeyboard();
                            break;
                        case "Change difficulty":
                            await SendInlineDifficultySetupKeyboard();
                            break;
                        case "Unsubscribe from the bot":
                            action = UnsubscribeFromBot();
                            break;
                        case "Subscribe to the bot":
                            action = SendTimeReplyKeyboard();
                            break;
                        default:
                            action = null;
                            break;
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        if (action != null)
        {
            var sentMessage = await action;
            _logger.LogInformation($"The message with id = {sentMessage.MessageId}: \n {sentMessage.Text} \n was sent to user: " +
                                   $"Userid = {userId}, Username = {message.From?.Username} at {TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Europe/Moscow"))}");
        }

        return;

        async Task<Message> UnsubscribeFromBot()
        {
            //await _userSettingsRepository.SetTimeAsync(userId, TimeStamp.NotSet);
            await _usersRepository.UpdateUserAsync(
                new UserEntity
                {
                    TelegramUserId = userId,
                    TimeSetting = TimeStamp.NotSet
                });
        
            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: "You are unsubscribed from bot.",
                replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Subscribe to the bot")),
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> GetUserSettings()
        {
            //var userSettings = await _userSettingsRepository.GetAsync(userId);
            var user = await _usersRepository.GetUserAsync(userId);
        
            var time = user.TimeSetting switch
            {
                TimeStamp.Ten => "10:00",
                TimeStamp.Fourteen => "14:00",
                TimeStamp.Sixteen => "18:00",
                TimeStamp.TwentyTwo => "22:00",
                _ => "Not set"
            };

            var difficulty = user.Difficulty.ToString();
        
            var replyMessage = $"Time: {time}\n" +
                               $"Difficulty: {difficulty}";
        
            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessage,
                cancellationToken: cancellationToken);
        }
        
        async Task SendInlineDifficultySetupKeyboard()
    {
        const string replyMessage = "Tinker difficulty:";
        
        var m = await _botClient.SendTextMessageAsync(
            chatId: userId,
            text: replyMessage,
            cancellationToken: cancellationToken);
        
        InlineKeyboardMarkup easyInlineKeyboard = new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Add", $"EasyAdd"),
                    InlineKeyboardButton.WithCallbackData("Remove", $"EasyRemove"),
                },
            });
        
        var easyMessage = await _botClient.SendTextMessageAsync(
            chatId: userId,
            text: "Easy",
            replyMarkup: easyInlineKeyboard,
            cancellationToken: cancellationToken);

        InlineKeyboardMarkup mediumInlineKeyboard = new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Add", $"MediumAdd"),
                    InlineKeyboardButton.WithCallbackData("Remove", $"MediumRemove"),
                },
            });
        
        var mediumMessage = await _botClient.SendTextMessageAsync(
            chatId: userId,
            text: "Medium",
            replyMarkup: mediumInlineKeyboard,
            cancellationToken: cancellationToken);
        
        InlineKeyboardMarkup hardInlineKeyboard = new(
            new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Add", $"HardAdd"),
                    InlineKeyboardButton.WithCallbackData("Remove", $"HardRemove"),
                },
            });
        var hardMessage = await _botClient.SendTextMessageAsync(
            chatId: userId,
            text: "Hard",
            replyMarkup: hardInlineKeyboard,
            cancellationToken: cancellationToken);
        
        await Task.Factory.StartNew(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);

            await _botClient.DeleteMessageAsync(
                chatId: userId,
                messageId: m.MessageId,
                cancellationToken: cancellationToken);
            await _botClient.DeleteMessageAsync(
                chatId: userId,
                messageId: easyMessage.MessageId,
                cancellationToken: cancellationToken);
            await _botClient.DeleteMessageAsync(
                chatId: userId,
                messageId: mediumMessage.MessageId,
                cancellationToken: cancellationToken);
            await _botClient.DeleteMessageAsync(
                chatId: userId,
                messageId: hardMessage.MessageId,
                cancellationToken: cancellationToken);
        }, cancellationToken);
    }
        
        async Task<Message> SendTimeReplyKeyboard()
        {
            const string replyMessageText = "Choose time, when you want to receive notifications:";

            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                    new KeyboardButton[] { "10:00", "14:00" },
                    new KeyboardButton[] { "18:00", "22:00" },
                })
            {
                ResizeKeyboard = true
            };
            
            //_userStateRepository.AddOrUpdateState(userId, UserState.TimeSetup);
            await _usersRepository.UpdateUserAsync(
                new UserEntity
                {
                    TelegramUserId = userId,
                    State = UserState.TimeSetup
                });

            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> SendDifficultyOptionsReplyKeyboard()
        {
            const string messageText = "Choose problems difficulty:";

            ReplyKeyboardMarkup replyKeyboardMarkup = new(
                new[]
                {
                    new KeyboardButton[] { "Easy" },
                    new KeyboardButton[] { "Medium" },
                    new KeyboardButton[] { "Hard" },
                    new KeyboardButton[] { "Save difficulty" },
                })
            {
                ResizeKeyboard = true
            };
            
            await _usersRepository.UpdateUserAsync(
                new UserEntity
                {
                    TelegramUserId = userId,
                    State = UserState.DifficultySetup
                });

            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: messageText,
                replyMarkup: replyKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        
        static async Task<Message> RemoveKeyboard(
            ITelegramBotClient botClient,
            Message message,
            CancellationToken cancellationToken)
        {
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Removing keyboard",
                replyMarkup: new ReplyKeyboardRemove(),
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> Start()
        {
            const string replyMessageText = $"This bot sends coding problem from LeetCode at chosen time.";
        
            //userStateRepository.AddOrUpdateState(message.From!.Id, UserState.InitialSetUp);
            //await _userSettingsRepository.AddUserAsync(userId);
            await _usersRepository.AddUserAsync(new UserEntity
            {
                TelegramUserId = userId,
                State = UserState.NewUser,
                IsPremium = false,
                Difficulty = Difficulty.NotSet,
                TimeSetting = TimeStamp.NotSet
            });
        
            await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                cancellationToken: cancellationToken);

            return await SendTimeReplyKeyboard();
        }
        
        async Task<Message> SaveDifficulty()
        {
            //if ((await _userSettingsRepository.GetAsync(userId)).Difficulty == Difficulty.NotSet)
            if ((await _usersRepository.GetUserAsync(userId)).Difficulty == Difficulty.NotSet)
            {
                return await _botClient.SendTextMessageAsync(
                    chatId: userId,
                    text: "Please Select at least one difficulty",
                    cancellationToken: cancellationToken);
            }
            const string replyMessageText =
                $"You are registered. You will receive notifications at chosen time and with chosen difficulty.";
        
            //_userStateRepository.AddOrUpdateState(userId, UserState.ToNotificate);
            await _usersRepository.UpdateUserAsync(
                new UserEntity
                {
                    TelegramUserId = userId,
                    State = UserState.Registered
                });
            //_registeredUsersRepository.Add(userId);

            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                replyMarkup: _menuKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> SetupTime(TimeStamp time)
        {
            const string replyMessageText = $"Notification time chosen.";

            //await _userSettingsRepository.SetTimeAsync(userId, time);
            await _usersRepository.UpdateUserAsync(
                new UserEntity
                {
                    TelegramUserId = userId,
                    TimeSetting = time
                });
            
            var user = await _usersRepository.GetUserAsync(userId);
            if (user.Difficulty == Difficulty.NotSet)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: replyMessageText,
                    cancellationToken: cancellationToken);
                return await SendDifficultyOptionsReplyKeyboard();
            }

            //_userStateRepository.AddOrUpdateState(userId, UserState.ToNotificate);
            await _usersRepository.UpdateUserAsync(
                new UserEntity
                {
                    TelegramUserId = userId,
                    State = UserState.Registered
                });
        
            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                replyMarkup: _menuKeyboardMarkup,
                cancellationToken: cancellationToken);;
        }
    }

    private async Task<Message> AddDifficulty(long userId, Difficulty difficulty, CancellationToken cancellationToken)
        {
            var replyMessageText = $"Difficulty {difficulty} added.";
        
            //await _userSettingsRepository.AddDifficultyAsync(message.From!.Id, difficulty);
            var user = await _usersRepository.GetUserAsync(userId);
            await _usersRepository.UpdateUserAsync(
                new UserEntity
                {
                    TelegramUserId = userId,
                    Difficulty = difficulty switch
                    {
                        Difficulty.Easy => user.Difficulty | Difficulty.Easy,
                        Difficulty.Medium => user.Difficulty | Difficulty.Medium,
                        Difficulty.Hard => user.Difficulty | Difficulty.Hard,
                        _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
                    }
                });

            _logger.LogInformation($"Difficulty {difficulty} added to user with UserId: {userId}");

            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                cancellationToken: cancellationToken);
        }

    private async Task<Message> RemoveDifficulty(long userId, Difficulty difficulty, CancellationToken cancellationToken)
        {
            var replyMessageText = $"Difficulty {difficulty} removed.";
        
            //await _userSettingsRepository.RemoveDifficultyAsync(message.From!.Id, difficulty);
            var user = await _usersRepository.GetUserAsync(userId);
            await _usersRepository.UpdateUserAsync(
                new UserEntity
                {
                    TelegramUserId = userId,
                    Difficulty = difficulty switch
                    {
                        Difficulty.Easy => user.Difficulty & ~Difficulty.Easy,
                        Difficulty.Medium => user.Difficulty & ~Difficulty.Medium,
                        Difficulty.Hard => user.Difficulty & ~Difficulty.Hard,
                        _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
                    }
                });
            
            _logger.LogInformation($"Difficulty {difficulty} removed from user with UserId: {userId}");
            
            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                cancellationToken: cancellationToken);
        }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(
        CallbackQuery callbackQuery, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Received inline keyboard callback from: Userid = {callbackQuery.From.Id}, Username = {callbackQuery.From.Username} \n data: {callbackQuery.Data}");

        var data = callbackQuery.Data!.Split();
        var userId = callbackQuery.From.Id;

        switch (data[0])
        {
            case "EasyAdd":
                await AddDifficulty(userId, Difficulty.Easy, cancellationToken);
                break;
            case "EasyRemove":
                await RemoveDifficulty(userId, Difficulty.Easy, cancellationToken);
                break;
            case "MediumAdd":
                await AddDifficulty(userId, Difficulty.Medium, cancellationToken);
                break;
            case "MediumRemove":
                await RemoveDifficulty(userId, Difficulty.Medium, cancellationToken);
                break;
            case "HardAdd":
                await AddDifficulty(userId, Difficulty.Hard, cancellationToken);
                break;
            case "HardRemove":
                await RemoveDifficulty(userId, Difficulty.Hard, cancellationToken);
                break;
            case "ProblemSolved":
                var problemId = int.Parse(data[1]);
                
                await _solvedQuestionsRepository
                    .AddSolvedQuestionAsync(
                        userId, 
                        new SolvedQuestionsEntity{
                            QuestionId = problemId, 
                            Date = DateTime.UtcNow,
                        });
                
                await _botClient.DeleteMessageAsync(userId, callbackQuery.Message!.MessageId, cancellationToken);
                
                var reply = $"Problem {problemId} marked as solved.";
                
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: reply,
                    cancellationToken: cancellationToken);
                break;
            default:
                await _botClient.SendTextMessageAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    text: "Внутренняя ошибка. Попробуйте позже.",
                    cancellationToken: cancellationToken);
                throw new ArgumentException("Callback data is not valid.");
        }

        await _botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    #region Inline Mode

    private async Task BotOnInlineQueryReceived(InlineQuery inlineQuery, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline query from: {InlineQueryFromId}", inlineQuery.From.Id);

        InlineQueryResult[] results =
        {
            // displayed result
            new InlineQueryResultArticle(
                id: "1",
                title: "TgBots",
                inputMessageContent: new InputTextMessageContent("hello"))
        };

        await _botClient.AnswerInlineQueryAsync(
            inlineQueryId: inlineQuery.Id,
            results: results,
            cacheTime: 0,
            isPersonal: true,
            cancellationToken: cancellationToken);
    }

    private async Task BotOnChosenInlineResultReceived(ChosenInlineResult chosenInlineResult,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline result: {ChosenInlineResultId}", chosenInlineResult.ResultId);

        await _botClient.SendTextMessageAsync(
            chatId: chosenInlineResult.From.Id,
            text: $"You chose result with Id: {chosenInlineResult.ResultId}",
            cancellationToken: cancellationToken);
    }

    #endregion

#pragma warning disable IDE0060 // Remove unused parameter
#pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandlerAsync(Update update, CancellationToken cancellationToken)
#pragma warning restore RCS1163 // Unused parameter.
#pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}