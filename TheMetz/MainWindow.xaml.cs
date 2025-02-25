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

    private async Task LoadPrClosedData()
    {
        PrClosedResultsList.Items.Clear();

        PrClosedResultsList.Items.Add("Loading...");

        IEnumerable<KeyValuePair<string, int>> test = await _pullRequestStatsService.ShowClosedPrCounts(_numberOfDaysToFetch);

        PrClosedResultsList.Items.Clear();

        foreach (KeyValuePair<string, int> keyValuePair in test)
        {
            PrClosedResultsList.Items.Add($"{keyValuePair.Key}: {keyValuePair.Value}");
        }
    }

    private async Task LoadPrReviewedData()
    {
        PrReviewedResultsList.Items.Clear();

        PrReviewedResultsList.Items.Add("Loading...");

        IEnumerable<KeyValuePair<string, int>> test = await _pullRequestStatsService.ShowReviewedPrCounts(_numberOfDaysToFetch);

        PrReviewedResultsList.Items.Clear();

        foreach (KeyValuePair<string, int> keyValuePair in test)
        {
            PrReviewedResultsList.Items.Add($"{keyValuePair.Key}: {keyValuePair.Value}");
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
        
        PrOpenedResults.Document.Blocks.Clear();
            
        var selectedAuthor = PrOpenedResultsList.SelectedItem!.ToString();
            
        var authorParagraph = new Paragraph(new Run(selectedAuthor));
        PrOpenedResults.Document.Blocks.Add(authorParagraph);

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
            PrOpenedResults.Document.Blocks.Add(paragraph);
        }
        PrOpenedResultsList.SelectedItem = null;
    }
    
    private void PrClosedResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PrClosedResultsList.SelectedItem == null)
        {
            return;
        }
        
        PrClosedResults.Document.Blocks.Clear();
            
        var selectedAuthor = PrClosedResultsList.SelectedItem!.ToString();
            
        var authorParagraph = new Paragraph(new Run(selectedAuthor));
        PrClosedResults.Document.Blocks.Add(authorParagraph);

        int truncateIndex = selectedAuthor!.IndexOf(':');
        string authorName = selectedAuthor[..(truncateIndex == -1 ? selectedAuthor.Length : truncateIndex)];
        List<(string Title, string Url)> commentLinks = _pullRequestStatsService.GetDeveloperClosedPrLinks(authorName);

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
            PrClosedResults.Document.Blocks.Add(paragraph);
        }
        PrClosedResultsList.SelectedItem = null;
    }
    
    private void PrReviewedResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PrReviewedResultsList.SelectedItem == null)
        {
            return;
        }
        
        PrReviewedResults.Document.Blocks.Clear();
            
        var selectedAuthor = PrReviewedResultsList.SelectedItem!.ToString();
            
        var authorParagraph = new Paragraph(new Run(selectedAuthor));
        PrReviewedResults.Document.Blocks.Add(authorParagraph);

        int truncateIndex = selectedAuthor!.IndexOf(':');
        string authorName = selectedAuthor[..(truncateIndex == -1 ? selectedAuthor.Length : truncateIndex)];
        List<(string Title, string Url)> commentLinks = _pullRequestStatsService.GetDeveloperReviewedPrLinks(authorName);

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
            PrReviewedResults.Document.Blocks.Add(paragraph);
        }
        PrReviewedResultsList.SelectedItem = null;
    }
    
    private async void FetchPrCommentButtonClicked(object sender, RoutedEventArgs e)
    {
        await LoadPrCommentData();
    }
    
    private async void FetchOpenedPrsButtonClicked(object sender, RoutedEventArgs e)
    {
        await LoadPrOpenedData();
    }

    private async void FetchClosedPrsButtonClicked(object sender, RoutedEventArgs e)
    {
        await LoadPrClosedData();
    }

    private async void FetchReviewedPrsButtonClicked(object sender, RoutedEventArgs e)
    {
        await LoadPrReviewedData();
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}