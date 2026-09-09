using Avalonia.Controls;
using ScottPlot;

namespace FUN;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        // Donnés fictives 
        double[] dataX = [1, 2, 3, 4, 5];
        double[] dataY = [1, 4, 9, 16, 25];

        MyPlot.Plot.Add.Scatter(dataX, dataY);
        
        MyPlot.Plot.Title("graphique x");
        MyPlot.Plot.XLabel("Axe X");
        MyPlot.Plot.YLabel("Axe Y");

        IXAxis xAxis = MyPlot.Plot.Axes.Bottom;
        IYAxis yAxis = MyPlot.Plot.Axes.Left;
        
        // Limiter le zoom
        MyPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.MinimumSpan(
            xAxis: xAxis,
            yAxis: yAxis,
            xSpan: 1.0,  
            ySpan: 1.0
        ));

        // Limiter le zoom
        AxisLimits limits = new(-5, 10, -5, 30);
        MyPlot.Plot.Axes.Rules.Add(new ScottPlot.AxisRules.MaximumBoundary(
            xAxis: xAxis,
            yAxis: yAxis,
            limits: limits
        ));

        MyPlot.Refresh();
    }
}