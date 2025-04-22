using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TheMetz.Models;

namespace TheMetz.Partials;

public partial class CommentStats : UserControl
{
    private CommentStatsViewModel ViewModel => (CommentStatsViewModel)DataContext;

    public CommentStats()
    {
        InitializeComponent();
    }
    
    // private async Task LoadPrCommentData()
    // {
    //     PrReviewResultsList.Items.Clear();
    //
    //     PrReviewResultsList.Items.Add("Loading...");
    //
    //     IEnumerable<KeyValuePair<string, int>> test = await _pullRequestCommentService.ShowCommentCounts(_numberOfDaysToFetch);
    //
    //     PrReviewResultsList.Items.Clear();
    //
    //     foreach (KeyValuePair<string, int> keyValuePair in test)
    //     {
    //         PrReviewResultsList.Items.Add($"{keyValuePair.Key}: {keyValuePair.Value}");
    //     }
    // }

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
        List<(string Title, string Url)> commentLinks = ViewModel.PullRequestCommentService.GetDeveloperCommentLinks(authorName);

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
            PrReviewResults.Document.Blocks.Add(paragraph);
        }
        PrReviewResultsList.SelectedItem = null;
    }
    
    private async void FetchPrCommentButtonClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadPrCommentData(CommentStatsDaysControl.GetDaysSliderValue());
    }
}