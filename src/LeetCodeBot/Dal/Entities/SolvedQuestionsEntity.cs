namespace LeetCodeBot.Dal.Entities;

public record SolvedQuestionsEntity
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public int QuestionId { get; init; }
    public DateTime Date { get; init; }
}