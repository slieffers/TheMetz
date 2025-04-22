using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TheMetz.Services;

namespace TheMetz.Models;

public class PullRequestStatsViewModel
{
    public readonly IPullRequestStateChangeService PullRequestStateChangeService;

    public ObservableCollection<string> PrOpenedResults { get; } = new();
    public ObservableCollection<string> PrClosedResults { get; } = new();
    public ObservableCollection<string> PrReviewedResults { get; } = new();

    public PullRequestStatsViewModel(IPullRequestStateChangeService pullRequestStateChangeService)
    {
        PullRequestStateChangeService = pullRequestStateChangeService;
    }

    public async Task LoadPrOpenedData(int numberOfDaysToFetch)
    {
        PrOpenedResults.Clear();
        PrOpenedResults.Add("Loading...");
        IEnumerable<KeyValuePair<string, int>> opened =
            await PullRequestStateChangeService.ShowOpenedPrCounts(numberOfDaysToFetch);

        PrOpenedResults.Clear();
        foreach (var kvp in opened)
            PrOpenedResults.Add($"{kvp.Key}: {kvp.Value}");
    }

    public async Task LoadPrClosedData(int numberOfDaysToFetch)
    {
        PrClosedResults.Clear();

        PrClosedResults.Add("Loading...");

        IEnumerable<KeyValuePair<string, int>> test =
            await PullRequestStateChangeService.ShowClosedPrCounts(numberOfDaysToFetch);

        PrClosedResults.Clear();

        foreach (KeyValuePair<string, int> keyValuePair in test)
        {
            PrClosedResults.Add($"{keyValuePair.Key}: {keyValuePair.Value}");
        }
    }

    public async Task LoadPrReviewedData(int numberOfDaysToFetch)
    {
        PrReviewedResults.Clear();

        PrReviewedResults.Add("Loading...");

        IEnumerable<KeyValuePair<string, int>> test =
            await PullRequestStateChangeService.ShowReviewedPrCounts(numberOfDaysToFetch);

        PrReviewedResults.Clear();

        foreach (KeyValuePair<string, int> keyValuePair in test)
        {
            PrReviewedResults.Add($"{keyValuePair.Key}: {keyValuePair.Value}");
        }
    }
}