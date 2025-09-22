namespace TheMetz.Interfaces;

using TheMetz.FSharp;

public interface IPullRequestCommentService
{
    public Task<IEnumerable<KeyValuePair<string, (int totalReviews, int withComments)>>> ShowCommentCounts(int numberOfDays);
    public List<(string Title, string Url)> GetDeveloperCommentLinks(string developerName);
}