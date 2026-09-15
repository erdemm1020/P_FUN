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
        
        var checkBoxes = series.GroupBy(dp => dp.Value.CityName)
            .Select(grupByCities => 
            {
                double[] dates = grupByCities.Select(dp => dp.Timestamp.ToOADate()).ToArray();
                double[] temps = grupByCities.Select(dp => dp.Value.Temperature).ToArray();
                  
                var scatter = MyPlot.Plot.Add.Scatter(dates, temps);
                scatter.Label = grupByCities.Key;

                var checkBox = new CheckBox
                {
                    Content = grupByCities.Key,
                    IsChecked = true,
                    Foreground = Brush.Parse("#cdd6f4"),
                    Margin = new Thickness(12, 0)
                };

                checkBox.IsCheckedChanged += (sender, args) => 
                {
                    scatter.IsVisible = checkBox.IsChecked == true;
    
                    MyPlot.Refresh();
                };

                return checkBox;
            });

        CityCheckBoxContainer.Children.AddRange(checkBoxes);

        MyPlot.IsHitTestVisible = false; 
        
        MyPlot.Plot.Axes.DateTimeTicksBottom();
        MyPlot.Plot.ShowLegend(); 
        MyPlot.Refresh();
    }
}