using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using LeetCodeBot.Enums;
using LeetCodeBot.Models;
using LeetCodeBot.Services.Interfaces;

namespace LeetCodeBot.Services;

public class GetLeetcodeQuestionService : IGetLeetcodeQuestionService
{
    public async Task<ICollection<LeetcodeQuestionType>> GetLeetcodeQuestionsAsync(Difficulty difficulty = Difficulty.Any)
    {
        var graphQLHttpClient = new GraphQLHttpClient("https://leetcode.com/graphql", new NewtonsoftJsonSerializer());
        var questionsRequest = new GraphQLRequest
        {
            Query = @"
                    query problemsetQuestionList($categorySlug: String, $limit: Int, $skip: Int, $filters: QuestionListFilterInput) {
                        problemsetQuestionList: questionList(
                        categorySlug: $categorySlug
                        limit: $limit
                        skip: $skip
                        filters: $filters
                      ) {
                        questions: data {
                          acRate
                          difficulty
                          frontendQuestionId: questionFrontendId
                          paidOnly: isPaidOnly
                          title
                          titleSlug
                          topicTags {
                            slug
                          }
                        }
                      }
                    }
                ",
            Variables = new
            {
                categorySlug = "",
                filters = new
                {
                    //status = new List<string> { "Solved", "Attempted" }
                },
                limit = 3000,
                skip = 0
            }
        };

        var graphQLResponse = await graphQLHttpClient.SendQueryAsync<LeetcodeQuestionsResponseType>(questionsRequest);
        var questions = graphQLResponse.Data.ProblemsetQuestionList.Questions
            .Where(question => (question.Difficulty & difficulty) == question.Difficulty).ToArray();
        var count = questions.Count(type => type.Difficulty == Difficulty.Hard);
        return questions;
    }
}