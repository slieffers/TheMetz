using System.Collections.ObjectModel;
using TheMetz.Interfaces;

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

        Dictionary<string, FSharp.Models.ReviewCounts> comments = await PullRequestCommentService.ShowCommentCounts(numberOfDaysToFetch);

        PrReviewResults.Clear();

        foreach (KeyValuePair<string, FSharp.Models.ReviewCounts> comment in comments)
        {
            PrReviewResults.Add($"{comment.Key}: {comment.Value.TotalReviews} Reviews, {comment.Value.ReviewsWithComments} With Comments");
        }
    }
}