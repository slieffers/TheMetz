namespace TheMetz.Interfaces;

using TheMetz.FSharp;

public interface ICommentService
{
    public Task<Dictionary<string, Models.ReviewCounts>> ShowCommentCounts(int numberOfDays);
    public List<Models.Link> GetDeveloperCommentLinks(string developerName);
}