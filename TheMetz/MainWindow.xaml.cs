using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using Microsoft.VisualStudio.Services.WebApi;
using TheMetz.Services;

namespace TheMetz;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly IWorkItemService _workItemService;

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
    
    public MainWindow(IPullRequestCommentService pullRequestCommentService, IPullRequestStateChangeService pullRequestStatsService, IWorkItemService workItemService)
    {
        InitializeComponent();
        
        _pullRequestCommentService = pullRequestCommentService;
        _pullRequestStatsService = pullRequestStatsService;
        _workItemService = workItemService;

        DataContext = this;
    }
    
    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}