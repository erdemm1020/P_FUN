using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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
        
        RefreshPlot();

        MyPlot.IsHitTestVisible = true; 
    }

    private void RefreshPlot()
    {
        MyPlot.Plot.Clear();
        CityCheckBoxContainer.Children.Clear();

        var cityPlotsData = _series.Values
            .GroupBy(dp => dp.CityName)
            .Select(group => (
                CityName: group.Key,
                Dates: group.Select(dp => dp.DateData.ToOADate()).ToArray(),
                Temps: group.Select(dp => dp.Temperature).ToArray()
            ));

        var checkBoxes = cityPlotsData.Select(data =>
        {
            var scatter = MyPlot.Plot.Add.Scatter(data.Dates, data.Temps);
            scatter.LegendText = data.CityName;

            var checkBox = new CheckBox
            {
                Content = data.CityName,
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
        MyPlot.Plot.Axes.DateTimeTicksBottom();
        MyPlot.Plot.ShowLegend(); 
        MyPlot.Refresh();
    }

    private async void ImportFile_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionner un fichier de données",
            AllowMultiple = true
        });

        if (files is not { Count: > 0 }) return; 

        var newWeathers = files
            .Select(f => f.Path.LocalPath)
            .Where(path => !string.IsNullOrEmpty(path))
            .SelectMany(path => path!.ToLowerInvariant() switch
            {
                var p when p.EndsWith(".csv") => DataSerie<Weather>.FromCsv(path, Parser.ParseWeather).Values,
                var p when p.EndsWith(".json") => ParseJsonWeathers(path),
                _ => Enumerable.Empty<Weather>()
            })
            .ToList();

        if (newWeathers.Any())
        {
            _series = DataSerie<Weather>.From(_series.Values.Concat(newWeathers));
            RefreshPlot();
        }
    }

    private static IEnumerable<Weather> ParseJsonWeathers(string path)
    {
        try
        {
            string json = File.ReadAllText(path);
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<Weather>>(json, options) ?? Enumerable.Empty<Weather>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur JSON ({path}): {ex.Message}");
            return Enumerable.Empty<Weather>();
        }
    }

    private void ExportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ExportFormatComboBox.SelectedItem is not ComboBoxItem { Content: string format }) return;

        string basePath = AppDomain.CurrentDomain.BaseDirectory;

        switch (format)
        {
            case "PNG":
                string pngPath = Path.Combine(basePath, "graphique_export.png");
                MyPlot.Plot.SavePng(pngPath, 800, 600);
                Console.WriteLine($"Graphique exporté en PNG vers : {pngPath}");
                break;

            case "CSV":
                string csvPath = Path.Combine(basePath, "donnees_export.csv");
                var csvLines = _series.Values
                    .Select(w => $"{w.DateData:yyyy-MM-dd HH:mm:ss},{w.CityName},{w.Temperature.ToString(CultureInfo.InvariantCulture)},{w.Degres.ToString(CultureInfo.InvariantCulture)}")
                    .Prepend("Date,Ville,Temperature,Degres");
                
                File.WriteAllLines(csvPath, csvLines);
                Console.WriteLine($"Données exportées en CSV vers : {csvPath}");
                break;
        }
    }
}