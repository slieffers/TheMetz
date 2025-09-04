using System.Windows;
using System.Windows.Media;
using TheMetz.Interfaces;
using TheMetz.Models;
using TheMetz.Services;

namespace TheMetz;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IPullRequestService _pullRequestService;
    private readonly IWorkItemService _workItemService;

    public MainWindow(IPullRequestService pullRequestService, IWorkItemService workItemService, PullRequestStatsViewModel pullRequestStatsViewModel, 
        CommentStatsViewModel commentStatsViewModel, WorkItemStatsViewModel workItemStatsViewModel)
    {
        InitializeComponent();

        _pullRequestService = pullRequestService;
        _workItemService = workItemService;

        PullRequestStatsControl.DataContext = pullRequestStatsViewModel;
        CommentStatsControl.DataContext = commentStatsViewModel;
        WorkItemStatsControl.DataContext = workItemStatsViewModel;

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

    private async void TestWorkItemsButtonClicked(object sender, RoutedEventArgs e)
    {
        UpdatePullRequests.IsEnabled = false;
        CommentStatsControl.FetchPrReviewData.IsEnabled = false;
        
        LoadingLabel.Background = new SolidColorBrush(Colors.Plum);
        LoadingLabel.Content = "Testing Work Items...";
        
        await _workItemService.GetWorkItemsCompletedByDev("Liudmila Solovyeva");
        
        LoadingLabel.Content = "";
        LoadingLabel.Background = new SolidColorBrush(Colors.White);
        
        UpdatePullRequests.IsEnabled = true;
        CommentStatsControl.FetchPrReviewData.IsEnabled = true;
    }
}