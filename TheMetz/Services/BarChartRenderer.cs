using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using TheMetz.Interfaces;

namespace TheMetz.Services;

public class BarChartRenderer : IChartRenderer
{
    public void RenderChart(Canvas canvas, Dictionary<string, int> data)
    {
        canvas.Children.Clear();

        if (data.Count == 0)
            return;

        const double leftMargin = 80;
        const double rightMargin = 20;
        const double topMargin = 40;
        const double bottomMargin = 120;

        double canvasWidth = canvas.ActualWidth;
        double canvasHeight = canvas.ActualHeight;

        if (canvasWidth <= 0 || canvasHeight <= 0)
            return;

        double chartWidth = canvasWidth - leftMargin - rightMargin;
        double chartHeight = canvasHeight - topMargin - bottomMargin;

        int maxValue = data.Values.Max();
        if (maxValue == 0) maxValue = 1;

        int dataCount = data.Count;
        double barWidth = Math.Min(60, chartWidth / dataCount * 0.8);
        double spacing = chartWidth / dataCount;

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
            Canvas.SetLeft(label, leftMargin - 35);
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

            var bar = new Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                Fill = new SolidColorBrush(Color.FromRgb(70, 130, 180))
            };
            Canvas.SetLeft(bar, x);
            Canvas.SetTop(bar, y);
            canvas.Children.Add(bar);

            // Value label on top of bar
            var valueLabel = new TextBlock
            {
                Text = kvp.Value.ToString(),
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            Canvas.SetLeft(valueLabel, x + barWidth / 2 - 10);
            Canvas.SetTop(valueLabel, y - 20);
            canvas.Children.Add(valueLabel);

            // Developer name label (rotated)
            var nameLabel = new TextBlock
            {
                Text = kvp.Key,
                FontSize = 11
            };

            // Measure the text to calculate positioning
            nameLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double textWidth = nameLabel.DesiredSize.Width;

            // Calculate diagonal length needed to span bar width
            // For 45 degree rotation: diagonal = barWidth / cos(45) = barWidth * sqrt(2)
            double targetDiagonal = barWidth * Math.Sqrt(2);

            // Scale font size to fit bar width
            double scaleFactor = Math.Min(1.0, targetDiagonal / textWidth);
            nameLabel.FontSize = 11 * scaleFactor;

            var rotate = new RotateTransform(-45);
            nameLabel.LayoutTransform = rotate;

            Canvas.SetLeft(nameLabel, x);
            Canvas.SetTop(nameLabel, topMargin + chartHeight + 10);
            canvas.Children.Add(nameLabel);

            index++;
        }

        // Y-axis title
        var yTitle = new TextBlock
        {
            Text = "Work Items",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            RenderTransform = new RotateTransform(-90),
            RenderTransformOrigin = new Point(0, 0)
        };
        Canvas.SetLeft(yTitle, 15);
        Canvas.SetTop(yTitle, topMargin + chartHeight / 2 + 40);
        canvas.Children.Add(yTitle);

        // X-axis title
        var xTitle = new TextBlock
        {
            Text = "Developers",
            FontSize = 14,
            FontWeight = FontWeights.Bold
        };
        Canvas.SetLeft(xTitle, leftMargin + chartWidth / 2 - 40);
        Canvas.SetTop(xTitle, canvasHeight - 20);
        canvas.Children.Add(xTitle);
    }
}
