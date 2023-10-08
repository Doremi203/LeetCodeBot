using LeetCodeBot.Enums;

namespace LeetCodeBot.Models;

public class LeetcodeQuestionType
{
    public double AcRate { get; set; }
    public Difficulty Difficulty { get; set; }
    public int FrontendQuestionId { get; set; }
    public bool PaidOnly { get; set; }
    public string Title { get; set; }
    public string TitleSlug { get; set; }
    public List<TopicTagType> TopicTags { get; set; }
}