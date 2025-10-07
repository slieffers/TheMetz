using System.Windows.Controls;

namespace TheMetz.Interfaces;

public interface IChartRenderer
{
    void RenderChart(Canvas canvas, Dictionary<string, int> data);
}
