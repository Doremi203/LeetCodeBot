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
    private readonly IRegisteredUsersRepository _registeredUsersRepository;
    private readonly IUserStateRepository _userStateRepository;
    private readonly IUserSettingsRepository _userSettingsRepository;
    private readonly ISolvedQuestionsRepository _solvedQuestionsRepository;

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
        IRegisteredUsersRepository registeredUsersRepository,
        IUserStateRepository userStateRepository,
        IUserSettingsRepository userSettingsRepository,
        ISolvedQuestionsRepository solvedQuestionsRepository
    )
    {
        _botClient = botClient;
        _logger = logger;
        _registeredUsersRepository = registeredUsersRepository;
        _userStateRepository = userStateRepository;
        _userSettingsRepository = userSettingsRepository;
        _solvedQuestionsRepository = solvedQuestionsRepository;
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
        _logger.LogInformation("Receive message type: {MessageType}", message.Type);
        if (message.Text is not { } messageText)
            return;
        
        var userId = message.From!.Id;
        Task<Message>? action;

        switch (_userStateRepository.GetState(userId))
        {
            case UserState.NewUser:
                action = messageText switch
                {
                    "/start" => Start(),
                    _ => null
                };
                break;
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
                    "Easy" => AddDifficulty(Difficulty.Easy),
                    "Medium" => AddDifficulty(Difficulty.Medium),
                    "Hard" => AddDifficulty(Difficulty.Hard),
                    "Any" => AddDifficulty(Difficulty.Any),
                    "Save difficulty" => SaveDifficulty(),
                    _ => null
                };
                break;
            case UserState.ToNotificate:
            case UserState.Notificated:
                action = messageText switch
                {
                    "Get settings" => GetUserSettings(),
                    "Change notification time" => SendTimeReplyKeyboard(),
                    "Change difficulty" => SendInlineDifficultySetupKeyboard(),
                    "Unsubscribe from the bot" => UnsubscribeFromBot(),
                    "Subscribe to the bot" => SendTimeReplyKeyboard(),
                    _ => null
                };
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (action != null)
        {
            Message sentMessage = await action;
            _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
        }
        
        async Task<Message> UnsubscribeFromBot()
        {
            await _userSettingsRepository.SetTimeAsync(userId, TimeStamp.NotSet);
        
            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: "You are unsubscribed from bot.",
                replyMarkup: new ReplyKeyboardMarkup(new KeyboardButton("Subscribe to the bot")),
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> GetUserSettings()
        {
            var userSettings = await _userSettingsRepository.GetAsync(userId);
        
            var time = userSettings.Time switch
            {
                TimeStamp.Ten => "10:00",
                TimeStamp.Fourteen => "14:00",
                TimeStamp.Sixteen => "18:00",
                TimeStamp.TwentyTwo => "22:00",
                _ => "Not set"
            };

            var difficulty = userSettings.Difficulty.ToString();
        
            var replyMessage = $"Time: {time}\n" +
                               $"Difficulty: {difficulty}";
        
            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessage,
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> SendInlineDifficultySetupKeyboard()
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
        
        return m;
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
            
            _userStateRepository.AddOrUpdateState(userId, UserState.TimeSetup);

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
                    new KeyboardButton[] { "Any" },
                    new KeyboardButton[] { "Save difficulty" },
                })
            {
                ResizeKeyboard = true
            };

            _userStateRepository.AddOrUpdateState(userId, UserState.DifficultySetup);

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
            await _userSettingsRepository.AddUserAsync(userId);
        
            await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                cancellationToken: cancellationToken);

            return await SendTimeReplyKeyboard();
        }
        
        async Task<Message> SaveDifficulty()
        {
            if ((await _userSettingsRepository.GetAsync(userId)).Difficulty == Difficulty.NotSet)
            {
                return await _botClient.SendTextMessageAsync(
                    chatId: userId,
                    text: "Please Select at least one difficulty",
                    cancellationToken: cancellationToken);
            }
            const string replyMessageText =
                $"You are registered. You will receive notifications at chosen time and with chosen difficulty.";
        
            _userStateRepository.AddOrUpdateState(userId, UserState.ToNotificate);
            _registeredUsersRepository.Add(userId);

            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                replyMarkup: _menuKeyboardMarkup,
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> AddDifficulty(Difficulty difficulty)
        {
            var replyMessageText = $"Difficulty {difficulty} added.";
        
            await _userSettingsRepository.AddDifficultyAsync(message.From!.Id, difficulty);

            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> RemoveDifficulty(Difficulty difficulty)
        {
            var replyMessageText = $"Difficulty {difficulty} removed.";
        
            await _userSettingsRepository.RemoveDifficultyAsync(message.From!.Id, difficulty);

            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                cancellationToken: cancellationToken);
        }
        
        async Task<Message> SetupTime(TimeStamp time)
        {
            const string replyMessageText = $"Notification time chosen.";

            await _userSettingsRepository.SetTimeAsync(userId, time);
        
            if (!_registeredUsersRepository.Contains(userId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: replyMessageText,
                    cancellationToken: cancellationToken);
                return await SendDifficultyOptionsReplyKeyboard();
            }

            _userStateRepository.AddOrUpdateState(userId, UserState.ToNotificate);
        
            return await _botClient.SendTextMessageAsync(
                chatId: userId,
                text: replyMessageText,
                replyMarkup: _menuKeyboardMarkup,
                cancellationToken: cancellationToken);;
        }
    }

    // Process Inline Keyboard callback data
    private async Task BotOnCallbackQueryReceived(
        CallbackQuery callbackQuery, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Received inline keyboard callback from: {CallbackQueryId}", callbackQuery.Id);

        var data = callbackQuery.Data!.Split();
        var userId = callbackQuery.From.Id;
        
        Task? action;
        string? reply;
        switch (data[0])
        {
            case "EasyAdd":
                action = _userSettingsRepository.AddDifficultyAsync(userId, Difficulty.Easy);
                reply = "Difficulty Easy added.";
                break;
            case "EasyRemove":
                action = _userSettingsRepository.RemoveDifficultyAsync(userId, Difficulty.Easy);
                reply = "Difficulty Easy removed.";
                break;
            case "MediumAdd":
                action = _userSettingsRepository.AddDifficultyAsync(userId, Difficulty.Medium);
                reply = "Difficulty Medium added.";
                break;
            case "MediumRemove":
                action = _userSettingsRepository.RemoveDifficultyAsync(userId, Difficulty.Medium);
                reply = "Difficulty Medium removed.";
                break;
            case "HardAdd":
                action = _userSettingsRepository.AddDifficultyAsync(userId, Difficulty.Hard);
                reply = "Difficulty Hard added.";
                break;
            case "HardRemove":
                action = _userSettingsRepository.RemoveDifficultyAsync(userId, Difficulty.Hard);
                reply = "Difficulty Hard removed.";
                break;
            case "ProblemSolved":
                var problemId = int.Parse(data[1]);
                action = _solvedQuestionsRepository.AddSolvedQuestionAsync(userId, problemId);
                await _botClient.DeleteMessageAsync(userId, callbackQuery.Message!.MessageId, cancellationToken);
                reply = $"Problem {problemId} marked as solved.";
                break;
            default:
                throw new ArgumentException();
        }

        await action;
        
        await _botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: reply,
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
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", ErrorMessage);

        // Cooldown in case of network connection error
        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
    }
}