using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.VisualStudio.Services.WebApi;
using TheMetz.Services;

namespace TheMetz;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IWorkItemService _workItemService;
    private readonly IPullRequestService _pullRequestService;

    private int _numberOfDaysToFetch = 0;

    public event PropertyChangedEventHandler PropertyChanged;

    public int NumberOfDaysToFetch
    {
        get => _numberOfDaysToFetch;
        set
        {
            if (_numberOfDaysToFetch != value)
            {
                _numberOfDaysToFetch = value;
                OnPropertyChanged();
            }
        }
    }

    public MainWindow(IPullRequestCommentService pullRequestCommentService,
        IPullRequestStateChangeService pullRequestStatsService, IWorkItemService workItemService,
        IPullRequestService pullRequestService)
    {
        InitializeComponent();

        _pullRequestCommentService = pullRequestCommentService;
        _pullRequestStatsService = pullRequestStatsService;
        _workItemService = workItemService;
        _pullRequestService = pullRequestService;

        DataContext = this;
    }

    private async void UpdatePullRequestsButtonClicked(object sender, RoutedEventArgs e)
    {
        UpdatePullRequests.IsEnabled = false;
        FetchPrReviewData.IsEnabled = false;
        
        LoadingLabel.Background = new SolidColorBrush(Colors.Plum);
        LoadingLabel.Content = "Updating Pull Requests...";
        
        await _pullRequestService.UpdateAdoPullRequests();
        
        LoadingLabel.Content = "";
        LoadingLabel.Background = new SolidColorBrush(Colors.White);
        
        UpdatePullRequests.IsEnabled = true;
        FetchPrReviewData.IsEnabled = true;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}