using System.Windows;
using System.Windows.Media;
using TheMetz.Interfaces;
using TheMetz.Models;

namespace TheMetz;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IPullRequestService _pullRequestService;

    public MainWindow(IPullRequestService pullRequestService, PullRequestStatsViewModel pullRequestStatsViewModel, 
        CommentStatsViewModel commentStatsViewModel, WorkItemStatsViewModel workItemStatsViewModel, IndividualStatsViewModel individualStatsViewModel)
    {
        InitializeComponent();

        _pullRequestService = pullRequestService;

        PullRequestStatsControl.DataContext = pullRequestStatsViewModel;
        CommentStatsControl.DataContext = commentStatsViewModel;
        WorkItemStatsControl.DataContext = workItemStatsViewModel;
        IndividualStatsControl.DataContext = individualStatsViewModel;

        DataContext = this;
    }

    private async void UpdatePullRequestsButtonClicked(object sender, RoutedEventArgs e)
    {
        UpdatePullRequests.IsEnabled = false;
        CommentStatsControl.FetchPrReviewData.IsEnabled = false;
        
        LoadingLabel.Background = new SolidColorBrush(Colors.Plum);
        LoadingLabel.Content = "Updating Pull Requests...";
        
        await _pullRequestService.UpdateAdoPullRequests();
        
        LoadingLabel.Content = "";
        LoadingLabel.Background = new SolidColorBrush(Colors.White);
        
        UpdatePullRequests.IsEnabled = true;
        CommentStatsControl.FetchPrReviewData.IsEnabled = true;
    }
}