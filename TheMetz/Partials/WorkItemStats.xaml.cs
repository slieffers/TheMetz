using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using TheMetz.Models;

namespace TheMetz.Partials;

public partial class WorkItemStats : UserControl
{
    private WorkItemStatsViewModel ViewModel => (WorkItemStatsViewModel)DataContext;

    public WorkItemStats()
    {
        InitializeComponent();
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
            RenderChart();
            WorkItemChartContainer.Visibility = Visibility.Visible;
            WorkItemChartLabel.Visibility = Visibility.Visible;
        }
        else
        {
            WorkItemChartContainer.Visibility = Visibility.Collapsed;
            WorkItemChartLabel.Visibility = Visibility.Collapsed;
        }
    }

    private void RenderChart()
    {
        WorkItemChart.Children.Clear();

        if (ViewModel.DeveloperWorkItemCounts.Count == 0)
            return;

        const double leftMargin = 80;
        const double rightMargin = 20;
        const double topMargin = 40;
        const double bottomMargin = 120;

        double canvasWidth = WorkItemChart.ActualWidth;
        double canvasHeight = WorkItemChart.ActualHeight;

        if (canvasWidth == 0 || canvasHeight == 0)
        {
            WorkItemChart.Loaded += (_, _) => RenderChart();
            return;
        }

        double chartWidth = canvasWidth - leftMargin - rightMargin;
        double chartHeight = canvasHeight - topMargin - bottomMargin;

        int maxValue = ViewModel.DeveloperWorkItemCounts.Values.Max();
        if (maxValue == 0) maxValue = 1;

        int developerCount = ViewModel.DeveloperWorkItemCounts.Count;
        double barWidth = Math.Min(60, chartWidth / developerCount * 0.8);
        double spacing = chartWidth / developerCount;

        // Draw Y-axis
        var yAxis = new System.Windows.Shapes.Line
        {
            X1 = leftMargin,
            Y1 = topMargin,
            X2 = leftMargin,
            Y2 = topMargin + chartHeight,
            Stroke = System.Windows.Media.Brushes.Black,
            StrokeThickness = 2
        };
        WorkItemChart.Children.Add(yAxis);

        // Draw X-axis
        var xAxis = new System.Windows.Shapes.Line
        {
            X1 = leftMargin,
            Y1 = topMargin + chartHeight,
            X2 = leftMargin + chartWidth,
            Y2 = topMargin + chartHeight,
            Stroke = System.Windows.Media.Brushes.Black,
            StrokeThickness = 2
        };
        WorkItemChart.Children.Add(xAxis);

        // Draw Y-axis labels and gridlines
        int gridLines = 5;
        for (int i = 0; i <= gridLines; i++)
        {
            double value = maxValue * i / gridLines;
            double y = topMargin + chartHeight - (chartHeight * i / gridLines);

            var gridLine = new System.Windows.Shapes.Line
            {
                X1 = leftMargin,
                Y1 = y,
                X2 = leftMargin + chartWidth,
                Y2 = y,
                Stroke = System.Windows.Media.Brushes.LightGray,
                StrokeThickness = 1,
                StrokeDashArray = new System.Windows.Media.DoubleCollection { 2, 2 }
            };
            WorkItemChart.Children.Add(gridLine);

            var label = new TextBlock
            {
                Text = ((int)value).ToString(),
                FontSize = 12
            };
            Canvas.SetLeft(label, leftMargin - 35);
            Canvas.SetTop(label, y - 8);
            WorkItemChart.Children.Add(label);
        }

        // Draw bars and labels
        int index = 0;
        foreach (var kvp in ViewModel.DeveloperWorkItemCounts)
        {
            double x = leftMargin + spacing * index + (spacing - barWidth) / 2;
            double barHeight = (kvp.Value / (double)maxValue) * chartHeight;
            double y = topMargin + chartHeight - barHeight;

            var bar = new System.Windows.Shapes.Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(70, 130, 180))
            };
            Canvas.SetLeft(bar, x);
            Canvas.SetTop(bar, y);
            WorkItemChart.Children.Add(bar);

            // Value label on top of bar
            var valueLabel = new TextBlock
            {
                Text = kvp.Value.ToString(),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(valueLabel, x + barWidth / 2 - 10);
            Canvas.SetTop(valueLabel, y - 20);
            WorkItemChart.Children.Add(valueLabel);

            // Developer name label (rotated)
            var nameLabel = new TextBlock
            {
                Text = kvp.Key,
                FontSize = 11,
                RenderTransform = new RotateTransform(-45),
                RenderTransformOrigin = new System.Windows.Point(0, 0)
            };
            Canvas.SetLeft(nameLabel, leftMargin + spacing * index + spacing / 2);
            Canvas.SetTop(nameLabel, topMargin + chartHeight + 10);
            WorkItemChart.Children.Add(nameLabel);

            index++;
        }

        // Y-axis title
        var yTitle = new TextBlock
        {
            Text = "Work Items",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            RenderTransform = new RotateTransform(-90),
            RenderTransformOrigin = new System.Windows.Point(0, 0)
        };
        Canvas.SetLeft(yTitle, 15);
        Canvas.SetTop(yTitle, topMargin + chartHeight / 2 + 40);
        WorkItemChart.Children.Add(yTitle);

        // X-axis title
        var xTitle = new TextBlock
        {
            Text = "Developers",
            FontSize = 14,
            FontWeight = FontWeights.Bold
        };
        Canvas.SetLeft(xTitle, leftMargin + chartWidth / 2 - 40);
        Canvas.SetTop(xTitle, canvasHeight - 20);
        WorkItemChart.Children.Add(xTitle);
    }
}