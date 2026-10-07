using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        
        var allData = new List<Weather>();

        string importsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FichierIMport");
        if (Directory.Exists(importsDir))
        {
            // parcours de chaque fichier qui se trouve dans le dossier import
            foreach (var file in Directory.GetFiles(importsDir))
            {
                try
                {
                    // détermination du parser en fonction de l'extension 
                    var parsed = Path.GetExtension(file).ToLowerInvariant() switch
                    {
                        ".csv" => DataSerie<Weather>.FromCsv(file, Parser.ParseWeather).Values,
                        _ => Enumerable.Empty<Weather>()
                    };
                    // ajout des données dans la liste 
                    allData.AddRange(parsed);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur de chargement ({file}) : {ex.Message}");
                }
            }
        }

        // tri des données pour enelver les doublons
        var combinedValues = allData
            .GroupBy(w => new { w.DateData, w.CityName })
            .Select(g => g.First())
            .OrderBy(w => w.DateData);

        // céraition de la serie de données à partir des valeurs sans doublons
        _series = DataSerie<Weather>.From(combinedValues);
        
        Console.WriteLine($"Nombre d'éléments : {_series.Count}");
        
        RefreshPlot();

        // interaction dans le graphiuqe
        MyPlot.IsHitTestVisible = true; 
    }



    /// <summary>
    /// mets a jour tout le graphique et re créé les checkboxes pour chaque villes
    /// </summary>
    private void RefreshPlot()
    {
        // nettoyage de l'ancien affichage
        MyPlot.Plot.Clear();
        CityCheckBoxContainer.Children.Clear();

        // regroupement des données par villes
        var cityPlotsData = _series.Values
            .GroupBy(dp => dp.CityName)
            .Select(group => (
                CityName: group.Key,
                Dates: group.Select(dp => dp.DateData.ToOADate()).ToArray(),
                Temps: group.Select(dp => dp.Temperature).ToArray()
            ));

        // génération des courbes et de leurs checkBoxes
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

            //
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
            Title = "Selectionner un fichier",
            AllowMultiple = true
        });

        if (files is not { Count: > 0 }) return; 

        string importsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FichierIMport");
        Directory.CreateDirectory(importsDir);

        var allNewWeathers = new List<Weather>();

        foreach (var file in files)
        {
            try
            {
                string sourcePath = file.Path.LocalPath;
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) continue;

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string destPath = Path.Combine(importsDir, $"{timestamp}_{Path.GetFileName(sourcePath)}");

                File.Copy(sourcePath, destPath, true);
                Console.WriteLine($"Fichier copiee : {destPath}");

                var parsedWeathers = Path.GetExtension(destPath).ToLowerInvariant() switch
                {
                    ".csv" => DataSerie<Weather>.FromCsv(destPath, Parser.ParseWeather).Values,
                    _ => Enumerable.Empty<Weather>()
                };

                allNewWeathers.AddRange(parsedWeathers);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur ({file.Name}) : {ex.Message}");
            }
        }

        if (allNewWeathers.Any())
        {
            var combinedValues = _series.Values
                .Concat(allNewWeathers)
                .GroupBy(w => new { w.DateData, w.CityName })
                .Select(g => g.First())
                .OrderBy(w => w.DateData);

            _series = DataSerie<Weather>.From(combinedValues);
            RefreshPlot();
            Console.WriteLine($"Importatoion terminée. Total : {_series.Count}");
        }
    }

    private void ExportButton_Click(object? sender, RoutedEventArgs e)
    {
        if (ExportFormatComboBox.SelectedItem is not ComboBoxItem { Content: string format }) return;

        string basePath = AppDomain.CurrentDomain.BaseDirectory;
        string exportsDir = Path.Combine(basePath, "ExportedFiles");
        Directory.CreateDirectory(exportsDir);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

        switch (format)
        {
            case "PNG":
                string pngPath = Path.Combine(exportsDir, $"{timestamp}_graphique_export.png");
                MyPlot.Plot.SavePng(pngPath, 800, 600);
                Console.WriteLine($"Export PNG : {pngPath}");
                break;

            case "CSV":
                string csvPath = Path.Combine(exportsDir, $"{timestamp}_donnees_export.csv");
                var csvLines = _series.Values
                    .Select(w => $"{w.DateData:yyyy-MM-dd HH:mm:ss},{w.CityName},{w.Temperature.ToString(CultureInfo.InvariantCulture)},{w.Degres.ToString(CultureInfo.InvariantCulture)}")
                    .Prepend("Date,Ville,Temperature,Degres");

                File.WriteAllLines(csvPath, csvLines);
                Console.WriteLine($"Export CSV : {csvPath}");
                break;
        }
    }
    private void RefreshPlot_Click(object? sender, RoutedEventArgs e)
    {
        RefreshPlot();
    }
}