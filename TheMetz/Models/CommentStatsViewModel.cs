using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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

        IEnumerable<KeyValuePair<string, int>> test = await PullRequestCommentService.ShowCommentCounts(numberOfDaysToFetch);

        PrReviewResults.Clear();

        foreach (KeyValuePair<string, int> keyValuePair in test)
        {
            PrReviewResults.Add($"{keyValuePair.Key}: {keyValuePair.Value}");
        }
    }
}