using System.Collections.ObjectModel;
using TheMetz.Services;

namespace TheMetz.Models;

public class CommentStatsViewModel
{
    public readonly IPullRequestCommentService PullRequestCommentService;

    public ObservableCollection<string> PrReviewResults { get; } = new();

    public CommentStatsViewModel(IPullRequestCommentService pullRequestCommentService)
    {
        PullRequestCommentService = pullRequestCommentService;
    }

    public async Task LoadPrCommentData(int numberOfDaysToFetch)
    {
        PrReviewResults.Clear();

        PrReviewResults.Add("Loading...");

        IEnumerable<KeyValuePair<string, (int totalReviews, int commentCount)>> comments = await PullRequestCommentService.ShowCommentCounts(numberOfDaysToFetch);

        PrReviewResults.Clear();

        foreach (KeyValuePair<string, (int totalComments, int commentCount)> keyValuePair in comments)
        {
            PrReviewResults.Add($"{keyValuePair.Key}: {keyValuePair.Value.totalComments} Reviews, {keyValuePair.Value.commentCount} With Comments");
        }
    }
}