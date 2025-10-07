using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TheMetz.Models;

namespace TheMetz.Partials;

public partial class IndividualStats : UserControl
{
    private IndividualStatsViewModel ViewModel => (IndividualStatsViewModel)DataContext;

    public IndividualStats()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadTeamMembers();
    }

    private async Task LoadTeamMembers()
    {
        TeamMemberComboBox.IsEnabled = false;

        try
        {
            await ViewModel.LoadTeamMembers();
        }
        finally
        {
            TeamMemberComboBox.IsEnabled = true;
        }
    }

    private void ClearAllFields()
    {
        CommentStatsText.Text = "";
        PrReviewDetails.Document.Blocks.Clear();
        WorkItemDetails.Document.Blocks.Clear();
        PrOpenedStatsText.Text = "";
        PrClosedStatsText.Text = "";
        PrReviewedStatsText.Text = "";
    }

    private async void TeamMemberComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (TeamMemberComboBox.SelectedItem == null || TeamMemberComboBox.SelectedItem.ToString() == "Loading...")
        {
            return;
        }

        string selectedMember = TeamMemberComboBox.SelectedItem.ToString()!;

        // Clear all fields immediately
        ClearAllFields();

        // Show loading indicator
        TeamMemberComboBox.IsEnabled = false;
        LoadingLabel.Visibility = Visibility.Visible;

        try
        {
            // Fetch data for selected member
            await ViewModel.LoadMemberData(selectedMember, IndividualStatsDaysControl.GetDaysSliderValue());
        }
        finally
        {
            LoadingLabel.Visibility = Visibility.Collapsed;
            TeamMemberComboBox.IsEnabled = true;
        }

        // Update Comment Stats
        var commentStats = ViewModel.GetCommentStats(selectedMember);
        if (commentStats != null)
        {
            CommentStatsText.Text =
                $"{commentStats.TotalReviews} Total Reviews, {commentStats.ReviewsWithComments} With Comments";

            PrReviewDetails.Document.Blocks.Clear();
            var commentLinks = ViewModel.GetCommentLinks(selectedMember);
            foreach (var linkInfo in commentLinks)
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
                PrReviewDetails.Document.Blocks.Add(paragraph);
            }
        }
        else
        {
            CommentStatsText.Text = "No review data";
            PrReviewDetails.Document.Blocks.Clear();
        }

        // Update Work Item Stats
        var workItems = ViewModel.GetWorkItems(selectedMember);
        WorkItemDetails.Document.Blocks.Clear();
        if (workItems.Any())
        {
            var headerParagraph = new Paragraph(new Run($"Total Work Items: {workItems.Count}"))
            {
                FontWeight = FontWeights.Bold
            };
            WorkItemDetails.Document.Blocks.Add(headerParagraph);

            foreach (var workItem in workItems)
            {
                var paragraph = new Paragraph();
                var hyperlink = new Hyperlink(new Run($"#{workItem.Id} - {"TODO: ADD TITLE"}"))
                {
                    NavigateUri = new Uri(workItem.Url),
                    Cursor = Cursors.Hand
                };

                hyperlink.PreviewMouseLeftButtonDown += (_, _) =>
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = workItem.Url,
                        UseShellExecute = true
                    });
                };

                paragraph.Inlines.Add(hyperlink);
                WorkItemDetails.Document.Blocks.Add(paragraph);
            }
        }
        else
        {
            WorkItemDetails.Document.Blocks.Add(new Paragraph(new Run("No work items")));
        }

        // Update PR Stats
        var prOpenedCount = ViewModel.GetPrOpenedCount(selectedMember);
        PrOpenedStatsText.Text = prOpenedCount > 0 ? $"{prOpenedCount} PRs" : "No PRs opened";

        var prClosedCount = ViewModel.GetPrClosedCount(selectedMember);
        PrClosedStatsText.Text = prClosedCount > 0 ? $"{prClosedCount} PRs" : "No PRs closed";

        var prReviewedCount = ViewModel.GetPrReviewedCount(selectedMember);
        PrReviewedStatsText.Text = prReviewedCount > 0 ? $"{prReviewedCount} PRs" : "No PRs reviewed";
    }
}
