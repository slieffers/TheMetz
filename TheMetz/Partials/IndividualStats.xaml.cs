using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TheMetz.Interfaces;
using TheMetz.Models;
using TheMetz.Services;

namespace TheMetz.Partials;

public partial class IndividualStats : UserControl
{
    private IndividualStatsViewModel ViewModel => (IndividualStatsViewModel)DataContext;
    private int _lastFetchedDaysValue;
    private readonly IChartRenderer _chartRenderer;

    public IndividualStats()
    {
        InitializeComponent();
        _chartRenderer = new IndividualStatsChartRenderer();
        Loaded += async (_, _) => await LoadTeamMembers();
        IndividualStatsDaysControl.DaysSliderControl.ValueChanged += DaysSlider_ValueChanged;
    }

    private void DaysSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Show refresh button if days value has changed and a team member is selected
        if (TeamMemberComboBox.SelectedItem != null &&
            TeamMemberComboBox.SelectedItem.ToString() != "Loading..." &&
            (int)e.NewValue != _lastFetchedDaysValue)
        {
            RefreshButton.Visibility = Visibility.Visible;
        }
        else
        {
            RefreshButton.Visibility = Visibility.Collapsed;
        }
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
        PrOpenedDetails.Document.Blocks.Clear();
        PrClosedDetails.Document.Blocks.Clear();
        PrReviewedDetails.Document.Blocks.Clear();
        StatsChart.Children.Clear();
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
            int daysValue = IndividualStatsDaysControl.GetDaysSliderValue();
            await ViewModel.LoadMemberData(selectedMember, daysValue);
            _lastFetchedDaysValue = daysValue;
            RefreshButton.Visibility = Visibility.Collapsed;

            // Update all UI sections
            UpdateAllUIFields(selectedMember);
        }
        finally
        {
            LoadingLabel.Visibility = Visibility.Collapsed;
            TeamMemberComboBox.IsEnabled = true;
        }
    }

    private void UpdateAllUIFields(string selectedMember)
    {
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
                var hyperlink = new Hyperlink(new Run($"#{workItem.Id} - {(string)workItem.Fields["System.Title"]}"))
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

        // Update PR Opened Stats
        var prOpenedLinks = ViewModel.GetPrOpenedLinks(selectedMember);
        PrOpenedDetails.Document.Blocks.Clear();
        if (prOpenedLinks.Any())
        {
            foreach (var linkInfo in prOpenedLinks)
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
                PrOpenedDetails.Document.Blocks.Add(paragraph);
            }
        }
        else
        {
            PrOpenedDetails.Document.Blocks.Add(new Paragraph(new Run("No PRs opened")));
        }

        // Update PR Closed Stats
        var prClosedLinks = ViewModel.GetPrClosedLinks(selectedMember);
        PrClosedDetails.Document.Blocks.Clear();
        if (prClosedLinks.Any())
        {
            foreach (var linkInfo in prClosedLinks)
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
                PrClosedDetails.Document.Blocks.Add(paragraph);
            }
        }
        else
        {
            PrClosedDetails.Document.Blocks.Add(new Paragraph(new Run("No PRs closed")));
        }

        // Update PR Reviewed Stats
        var prReviewedLinks = ViewModel.GetPrReviewedLinks(selectedMember);
        PrReviewedDetails.Document.Blocks.Clear();
        if (prReviewedLinks.Any())
        {
            foreach (var linkInfo in prReviewedLinks)
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
                PrReviewedDetails.Document.Blocks.Add(paragraph);
            }
        }
        else
        {
            PrReviewedDetails.Document.Blocks.Add(new Paragraph(new Run("No PRs reviewed")));
        }

        // Render chart with all stats
        RenderChart(selectedMember);
    }

    private void RenderChart(string selectedMember)
    {
        var chartData = new Dictionary<string, int>();

        // Add work items count
        var workItems = ViewModel.GetWorkItems(selectedMember);
        chartData["Work Items"] = workItems.Count;

        // Add PR stats
        var prOpened = ViewModel.GetPrOpenedLinks(selectedMember);
        chartData["PRs Opened"] = prOpened.Count;

        var prClosed = ViewModel.GetPrClosedLinks(selectedMember);
        chartData["PRs Closed"] = prClosed.Count;

        var prReviewed = ViewModel.GetPrReviewedLinks(selectedMember);
        chartData["PRs Reviewed"] = prReviewed.Count;

        // Add review stats
        var commentStats = ViewModel.GetCommentStats(selectedMember);
        if (commentStats != null)
        {
            chartData["Total Reviews"] = commentStats.TotalReviews;
            chartData["Reviews w/ Comments"] = commentStats.ReviewsWithComments;
        }
        else
        {
            chartData["Total Reviews"] = 0;
            chartData["Reviews w/ Comments"] = 0;
        }

        _chartRenderer.RenderChart(StatsChart, chartData);
    }

    private void StatsChart_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (TeamMemberComboBox.SelectedItem != null &&
            TeamMemberComboBox.SelectedItem.ToString() != "Loading..." &&
            StatsChart.ActualWidth > 0 && StatsChart.ActualHeight > 0)
        {
            string selectedMember = TeamMemberComboBox.SelectedItem.ToString()!;
            RenderChart(selectedMember);
        }
    }

    private async void RefreshButton_Click(object sender, RoutedEventArgs e)
    {
        if (TeamMemberComboBox.SelectedItem == null || TeamMemberComboBox.SelectedItem.ToString() == "Loading...")
        {
            return;
        }

        string selectedMember = TeamMemberComboBox.SelectedItem.ToString()!;

        // Show loading indicator
        RefreshButton.IsEnabled = false;
        TeamMemberComboBox.IsEnabled = false;
        LoadingLabel.Visibility = Visibility.Visible;

        try
        {
            // Clear all fields
            ClearAllFields();

            // Fetch data with updated days value
            int daysValue = IndividualStatsDaysControl.GetDaysSliderValue();
            await ViewModel.LoadMemberData(selectedMember, daysValue);
            _lastFetchedDaysValue = daysValue;
            RefreshButton.Visibility = Visibility.Collapsed;

            // Update all UI sections (reuse existing logic)
            UpdateAllUIFields(selectedMember);
        }
        finally
        {
            LoadingLabel.Visibility = Visibility.Collapsed;
            RefreshButton.IsEnabled = true;
            TeamMemberComboBox.IsEnabled = true;
        }
    }
}
