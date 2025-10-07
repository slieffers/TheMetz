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
        List<FSharp.Models.Link> commentLinks = ViewModel.PullRequestCommentService.GetDeveloperCommentLinks(authorName);

        foreach (FSharp.Models.Link linkInfo in commentLinks)
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