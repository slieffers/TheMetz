using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using TheMetz.Interfaces;
using TheMetz.Models;
using TheMetz.Services;

namespace TheMetz.Partials;

public partial class WorkItemStats : UserControl
{
    private WorkItemStatsViewModel ViewModel => (WorkItemStatsViewModel)DataContext;
    private readonly IChartRenderer _chartRenderer;

    public WorkItemStats()
    {
        InitializeComponent();
        _chartRenderer = new BarChartRenderer();
    }
    
    private void WorkItemResultsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WorkItemResultsList.SelectedItem == null)
        {
            return;
        }
        
        WorkItemResults.Document.Blocks.Clear();
            
        var selectedAuthor = WorkItemResultsList.SelectedItem!.ToString();
            
        var authorParagraph = new Paragraph(new Run(selectedAuthor));
        WorkItemResults.Document.Blocks.Add(authorParagraph);

        int truncateIndex = selectedAuthor!.IndexOf(':');
        string authorName = selectedAuthor[..(truncateIndex == -1 ? selectedAuthor.Length : truncateIndex)];
        var workItemInfo = ViewModel.WorkItemService.GetWorkItemsCompletedByDevLinks(authorName);

        foreach (var workItemLink in workItemInfo.WorkItems)
        {
            var paragraph = new Paragraph();
            var url = workItemLink.Url.Replace("_apis/wit/workItems/", "_workItems/edit/");
            var hyperlink = new Hyperlink(new Run((string)workItemLink.Fields["System.Title"]))
            {
                NavigateUri = new Uri(url),
                Cursor = Cursors.Hand
            };
        
            hyperlink.PreviewMouseLeftButtonDown += (_, _) =>
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            };
        
            paragraph.Inlines.Add(hyperlink);
            WorkItemResults.Document.Blocks.Add(paragraph);
        }
        WorkItemResultsList.SelectedItem = null;
    }
    
    private async void FetchWorkItemButtonClicked(object sender, RoutedEventArgs e)
    {
        await ViewModel.LoadWorkItemData(WorkItemStatsDaysControl.GetDaysSliderValue());

        if (ViewModel.DeveloperWorkItemCounts.Count > 0)
        {
            WorkItemChart.Visibility = Visibility.Visible;
            WorkItemChartLabel.Visibility = Visibility.Visible;
            RenderChart();
        }
        else
        {
            WorkItemChart.Visibility = Visibility.Collapsed;
            WorkItemChartLabel.Visibility = Visibility.Collapsed;
        }
    }

    private void WorkItemChart_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (ViewModel.DeveloperWorkItemCounts.Count > 0)
        {
            RenderChart();
        }
    }

    private void RenderChart()
    {
        _chartRenderer.RenderChart(WorkItemChart, ViewModel.DeveloperWorkItemCounts);
    }
}