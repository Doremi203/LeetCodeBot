using LeetCodeBot.Enums;
using LeetCodeBot.Models;

namespace LeetCodeBot.Services.Interfaces;

interface IGetLeetcodeQuestionService
{
    Task<ICollection<LeetcodeQuestionType>> GetLeetcodeQuestionsAsync(Difficulty difficulty = Difficulty.Any);
}