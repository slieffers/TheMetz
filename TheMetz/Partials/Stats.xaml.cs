using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TheMetz.Services;

// ReSharper disable once CheckNamespace
namespace TheMetz;

public partial class MainWindow
{
    private readonly IPullRequestStateChangeService _pullRequestStatsService;

    private async Task LoadPrOpenedData()
    {
        await _workItemService.GetWorkItems([]);
        
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

            hyperlink.PreviewMouseLeftButtonDown += (_, _) =>
            {
                Process.Start(new ProcessStartInfo
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

            hyperlink.PreviewMouseLeftButtonDown += (_, _) =>
            {
                Process.Start(new ProcessStartInfo
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

            hyperlink.PreviewMouseLeftButtonDown += (_, _) =>
            {
                Process.Start(new ProcessStartInfo
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
}