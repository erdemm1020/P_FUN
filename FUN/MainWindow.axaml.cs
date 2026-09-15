using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using DataSeries;
using ScottPlot;

namespace FUN;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        DataSeries<DataPoint<Weather>> series = DataSeries<DataPoint<Weather>>.FromCsv("weather_data.csv", Parser.ParseWeather);
        Console.WriteLine($"Nombre d'éléments : {series.Count()}");
        
        var city = series.Select(dp => dp.Value.CityName).Distinct();
        Console.WriteLine($"Villes dans le csv : {string.Join(", ", city)}");

        var checkBoxes = city.Select(cityName => new CheckBox
        {
            Content = cityName,
            IsChecked = true,
            Foreground = Brush.Parse("#cdd6f4"),
            Margin = new Thickness(12, 0)
        });

        CityCheckBoxContainer.Children.AddRange(checkBoxes);
        
        series.GroupBy(dp => dp.Value.CityName)
            .ToList()
            .ForEach(grupByCities => 
            {
                double[] dates = grupByCities.Select(dp => dp.Timestamp.ToOADate()).ToArray();
                double[] temps = grupByCities.Select(dp => dp.Value.Temperature).ToArray();
                  
                var scatter = MyPlot.Plot.Add.Scatter(dates, temps);
                scatter.Label = grupByCities.Key;
            });
        
        MyPlot.Plot.Axes.DateTimeTicksBottom();
        
        MyPlot.Plot.ShowLegend(); 
        
        MyPlot.Refresh();
    }
}