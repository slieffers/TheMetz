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
    private readonly IPullRequestCommentService _pullRequestCommentService;
    private readonly IPullRequestStatsService _pullRequestStatsService;

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


    public MainWindow(IPullRequestCommentService pullRequestCommentService, IPullRequestStatsService pullRequestStatsService)
    {
        InitializeComponent();
        
        _pullRequestCommentService = pullRequestCommentService;
        _pullRequestStatsService = pullRequestStatsService;

        DataContext = this;
    }

    private async Task LoadPrCommentData()
    {
        PrReviewResultsList.Items.Clear();

        PrReviewResultsList.Items.Add("Loading...");

        IEnumerable<KeyValuePair<string, int>> test = await _pullRequestCommentService.ShowCommentCounts(_numberOfDaysToFetch);

        PrReviewResultsList.Items.Clear();

        foreach (KeyValuePair<string, int> keyValuePair in test)
        {
            PrReviewResultsList.Items.Add($"{keyValuePair.Key}: {keyValuePair.Value}");
        }
    }

    private async Task LoadPrOpenedData()
    {
        PrOpenedResultsList.Items.Clear();

        PrOpenedResultsList.Items.Add("Loading...");

        IEnumerable<KeyValuePair<string, int>> test = await _pullRequestStatsService.ShowOpenedPrCounts(_numberOfDaysToFetch);

        PrOpenedResultsList.Items.Clear();

        foreach (KeyValuePair<string, int> keyValuePair in test)
        {
            PrOpenedResultsList.Items.Add($"{keyValuePair.Key}: {keyValuePair.Value}");
        }
    }

    private void PrCommentResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PrReviewResultsList.SelectedItem == null)
        {
            return;
        }
        
        PrReviewResults.Document.Blocks.Clear();
            
        var selectedAuthor = PrReviewResultsList.SelectedItem!.ToString();
            
        var authorParagraph = new Paragraph(new Run(selectedAuthor));
        PrReviewResults.Document.Blocks.Add(authorParagraph);

        int truncateIndex = selectedAuthor!.IndexOf(':');
        string authorName = selectedAuthor[..(truncateIndex == -1 ? selectedAuthor.Length : truncateIndex)];
        List<(string Title, string Url)> commentLinks = _pullRequestCommentService.GetDeveloperCommentLinks(authorName);

        foreach ((string Title, string Url) linkInfo in commentLinks)
        {
            var paragraph = new Paragraph();
            var hyperlink = new Hyperlink(new Run(linkInfo.Title))
            {
                NavigateUri = new Uri(linkInfo.Url),
                Cursor = Cursors.Hand
            };

            hyperlink.PreviewMouseLeftButtonDown += (s, args) =>
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = linkInfo.Url,
                    UseShellExecute = true
                });
            };

            paragraph.Inlines.Add(hyperlink);
            PrReviewResults.Document.Blocks.Add(paragraph);
        }
        PrReviewResultsList.SelectedItem = null;
    }
    
    private void PrOpenedResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PrOpenedResultsList.SelectedItem == null)
        {
            return;
        }
        
        PrStatsResults.Document.Blocks.Clear();
            
        var selectedAuthor = PrOpenedResultsList.SelectedItem!.ToString();
            
        var authorParagraph = new Paragraph(new Run(selectedAuthor));
        PrStatsResults.Document.Blocks.Add(authorParagraph);

        int truncateIndex = selectedAuthor!.IndexOf(':');
        string authorName = selectedAuthor[..(truncateIndex == -1 ? selectedAuthor.Length : truncateIndex)];
        List<(string Title, string Url)> commentLinks = _pullRequestStatsService.GetDeveloperOpenedPrLinks(authorName);

        foreach ((string Title, string Url) linkInfo in commentLinks)
        {
            var paragraph = new Paragraph();
            var hyperlink = new Hyperlink(new Run(linkInfo.Title))
            {
                NavigateUri = new Uri(linkInfo.Url),
                Cursor = Cursors.Hand
            };

            hyperlink.PreviewMouseLeftButtonDown += (s, args) =>
            {
                System.Diagnostics.Process.Start(new ProcessStartInfo
                {
                    FileName = linkInfo.Url,
                    UseShellExecute = true
                });
            };

            paragraph.Inlines.Add(hyperlink);
            PrStatsResults.Document.Blocks.Add(paragraph);
        }
        PrOpenedResultsList.SelectedItem = null;
    }
    
    private async void FetchPrCommentButtonClicked(object sender, RoutedEventArgs e)
    {
        await LoadPrCommentData();
    }
    private async void FetchOpenedPrsButtonClicked(object sender, RoutedEventArgs e)
    {
        await LoadPrOpenedData();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}