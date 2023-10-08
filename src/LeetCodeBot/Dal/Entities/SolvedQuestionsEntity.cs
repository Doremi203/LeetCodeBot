namespace LeetCodeBot.Dal.Entities;

public record SolvedQuestionsEntity(
    Guid Id,
    long TelegramUserId, 
    int QuestionId,
    DateTime Time);