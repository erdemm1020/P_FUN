using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using DataSeries;
using ScottPlot;

namespace FUN;

public partial class MainWindow : Window
{
    private DataSerie<Weather> _series;

    public MainWindow()
    {
        InitializeComponent();

        _series = DataSerie<Weather>.FromCsv("weather_data.csv", Parser.ParseWeather);
        Console.WriteLine($"Nombre d'éléments : {_series.Count}");
        
        var checkBoxes = _series.Values.GroupBy(dp => dp.CityName)
            .Select(groupByCities => 
            {
                double[] dates = groupByCities.Select(dp => dp.DateData.ToOADate()).ToArray();
                double[] temps = groupByCities.Select(dp => dp.Temperature).ToArray();
                  
                var scatter = MyPlot.Plot.Add.Scatter(dates, temps);
                scatter.Label = groupByCities.Key;

                CheckBox checkBox = new CheckBox
                {
                    Content = groupByCities.Key,
                    IsChecked = true,
                    Foreground = Avalonia.Media.Brush.Parse("#cdd6f4"),
                    Margin = new Thickness(12, 0)
                };

                checkBox.IsCheckedChanged += (_, _) => 
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

    // Bouton d'export de graphique
    private void ExportButton_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ExportFormatComboBox.SelectedItem is ComboBoxItem selectedItem)
        {
            string format = selectedItem.Content?.ToString() ?? "";
            
            if (format == "PNG")
            {
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "graphique_export.png");
                MyPlot.Plot.SavePng(filePath, 800, 600);
                Console.WriteLine($"Graphique exporté en PNG vers : {filePath}");
            }
            else if (format == "CSV")
            {
                string filePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "donnees_export.csv");

                var csvLines = _series.Values
                    .Select(w => $"{w.DateData:yyyy-MM-dd HH:mm:ss},{w.CityName},{w.Temperature.ToString(System.Globalization.CultureInfo.InvariantCulture)},{w.Degres.ToString(System.Globalization.CultureInfo.InvariantCulture)}")
                    .Prepend("Date,Ville,Temperature,Degres");

                System.IO.File.WriteAllLines(filePath, csvLines);

                Console.WriteLine($"Données exportées en CSV vers : {filePath}");
            }
        }
    }
}