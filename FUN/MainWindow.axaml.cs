using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DataSeries;
using ScottPlot;

namespace FUN;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        var series = DataSeries<DataPoint<Weather>>.FromCsv("weather_data.csv", Parser.ParseWeather);
        Console.WriteLine($"Nombre d'éléments : {series.Count}");

        double[] dates = series.Select(dp => dp.Timestamp.ToOADate()).ToArray();
        double[] temps = series.Select(dp => dp.Value.Temperature).ToArray();

        var scatter = MyPlot.Plot.Add.Scatter(dates, temps);
        scatter.Label = "Temperature";
        scatter.Color = ScottPlot.Color.FromHex("#0284C7");

        MyPlot.Plot.Axes.DateTimeTicksBottom();
        MyPlot.Refresh();
    }

}