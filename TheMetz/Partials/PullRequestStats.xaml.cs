using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TheMetz.Models;

namespace TheMetz.Partials;

public partial class PullRequestStats : UserControl
{
    private PullRequestStatsViewModel ViewModel => (PullRequestStatsViewModel)DataContext;

    public PullRequestStats()
    {
        InitializeComponent();
        PrsOpenedDaysControl.DaysSliderControl.Value = 14;
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
        List<(string Title, string Url)> commentLinks = ViewModel.PullRequestStateChangeService.GetDeveloperOpenedPrLinks(authorName);

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
        List<(string Title, string Url)> commentLinks = ViewModel.PullRequestStateChangeService.GetDeveloperClosedPrLinks(authorName);

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
        List<(string Title, string Url)>
            commentLinks = ViewModel.PullRequestStateChangeService.GetDeveloperReviewedPrLinks(authorName);

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
        await ViewModel.LoadPrOpenedData(DaysControl.GetDaysSliderValue(PrsOpenedDaysControl.DaysSliderControl));
    }

    private async void FetchClosedPrsButtonClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadPrClosedData(DaysControl.GetDaysSliderValue(PrsClosedDaysControl.DaysSliderControl));
    }

    private async void FetchReviewedPrsButtonClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadPrReviewedData(DaysControl.GetDaysSliderValue(PrsReviewedDaysControl.DaysSliderControl));
    }
}