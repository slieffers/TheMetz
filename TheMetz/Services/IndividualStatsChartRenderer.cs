using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TheMetz.Interfaces;

namespace TheMetz.Services;

public class IndividualStatsChartRenderer : IChartRenderer
{
    public void RenderChart(Canvas canvas, Dictionary<string, int> data)
    {
        canvas.Children.Clear();

        if (data.Count == 0)
            return;

        const double leftMargin = 100;
        const double rightMargin = 40;
        const double topMargin = 40;
        const double bottomMargin = 80;

        double canvasWidth = canvas.ActualWidth;
        double canvasHeight = canvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0)
            return;

        double chartWidth = canvasWidth - leftMargin - rightMargin;
        double chartHeight = canvasHeight - topMargin - bottomMargin;

        int maxValue = data.Values.Max();
        if (maxValue == 0) maxValue = 1;

        int dataCount = data.Count;
        double barWidth = Math.Min(80, chartWidth / dataCount * 0.7);
        double spacing = chartWidth / dataCount;

        // Define color palette for different metrics
        var colors = new Dictionary<string, Color>
        {
            { "Work Items", Color.FromRgb(70, 130, 180) },      // Steel Blue
            { "PRs Opened", Color.FromRgb(60, 179, 113) },      // Medium Sea Green
            { "PRs Closed", Color.FromRgb(255, 140, 0) },       // Dark Orange
            { "PRs Reviewed", Color.FromRgb(220, 20, 60) },     // Crimson
            { "Total Reviews", Color.FromRgb(138, 43, 226) },   // Blue Violet
            { "Reviews w/ Comments", Color.FromRgb(255, 105, 180) } // Hot Pink
        };

        // Draw Y-axis
        var yAxis = new Line
        {
            X1 = leftMargin,
            Y1 = topMargin,
            X2 = leftMargin,
            Y2 = topMargin + chartHeight,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        canvas.Children.Add(yAxis);

        // Draw X-axis
        var xAxis = new Line
        {
            X1 = leftMargin,
            Y1 = topMargin + chartHeight,
            X2 = leftMargin + chartWidth,
            Y2 = topMargin + chartHeight,
            Stroke = Brushes.Black,
            StrokeThickness = 2
        };
        canvas.Children.Add(xAxis);

        // Draw Y-axis labels and gridlines
        int gridLines = 5;
        for (int i = 0; i <= gridLines; i++)
        {
            double value = maxValue * i / gridLines;
            double y = topMargin + chartHeight - (chartHeight * i / gridLines);

            var gridLine = new Line
            {
                X1 = leftMargin,
                Y1 = y,
                X2 = leftMargin + chartWidth,
                Y2 = y,
                Stroke = Brushes.LightGray,
                StrokeThickness = 1,
                StrokeDashArray = new DoubleCollection { 2, 2 }
            };
            canvas.Children.Add(gridLine);

            var label = new TextBlock
            {
                Text = ((int)value).ToString(),
                FontSize = 12
            };
            Canvas.SetLeft(label, leftMargin - 40);
            Canvas.SetTop(label, y - 8);
            canvas.Children.Add(label);
        }

        // Draw bars and labels
        int index = 0;
        foreach (var kvp in data)
        {
            double x = leftMargin + spacing * index + (spacing - barWidth) / 2;
            double barHeight = (kvp.Value / (double)maxValue) * chartHeight;
            double y = topMargin + chartHeight - barHeight;

            // Get color for this metric, default to steel blue if not found
            Color barColor = colors.TryGetValue(kvp.Key, out var color) ? color : Color.FromRgb(70, 130, 180);

            var bar = new Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                Fill = new SolidColorBrush(barColor)
            };
            Canvas.SetLeft(bar, x);
            Canvas.SetTop(bar, y);
            canvas.Children.Add(bar);

            // Value label on top of bar
            var valueLabel = new TextBlock
            {
                Text = kvp.Value.ToString(),
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };

            // Measure text to center it
            valueLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = valueLabel.DesiredSize.Width;

            Canvas.SetLeft(valueLabel, x + (barWidth - textWidth) / 2);
            Canvas.SetTop(valueLabel, y - 22);
            canvas.Children.Add(valueLabel);

            // Category label below X-axis
            var categoryLabel = new TextBlock
            {
                Text = kvp.Key,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Width = barWidth + 20
            };

            Canvas.SetLeft(categoryLabel, x + barWidth / 2 - (barWidth + 20) / 2);
            Canvas.SetTop(categoryLabel, topMargin + chartHeight + 10);
            canvas.Children.Add(categoryLabel);

            index++;
        }

        // Y-axis title
        var yTitle = new TextBlock
        {
            Text = "Count",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            RenderTransform = new RotateTransform(-90),
            RenderTransformOrigin = new Point(0, 0)
        };
        Canvas.SetLeft(yTitle, 20);
        Canvas.SetTop(yTitle, topMargin + chartHeight / 2 + 25);
        canvas.Children.Add(yTitle);

        // Chart title
        var title = new TextBlock
        {
            Text = "Activity Summary",
            FontSize = 16,
            FontWeight = FontWeights.Bold
        };
        Canvas.SetLeft(title, leftMargin + chartWidth / 2 - 60);
        Canvas.SetTop(title, 10);
        canvas.Children.Add(title);
    }
}
