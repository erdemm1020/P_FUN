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
    private DataSerie<Weather> _series = DataSerie<Weather>.From(Enumerable.Empty<Weather>());
    private Dictionary<string, string> _importedSourceFiles = new();

    public MainWindow()
    {
        InitializeComponent();
        
        ReloadData();

        // interaction dans le graphiuqe
        MyPlot.IsHitTestVisible = true; 
    }

    /// <summary>
    /// Recharge les données depuis le fichier et mets a jour toute l'application
    /// </summary>
    private void ReloadData()
    {
        string importsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FichierIMport");
        var allData = new List<Weather>();

        if (Directory.Exists(importsDir))
        {
            // Synchronisation des fichiers sources connus s'ils ont été modifiés
            foreach (var f in _importedSourceFiles)
            {
                string sourcePath = f.Key;
                string destPath = f.Value;
                try
                {
                    if (File.Exists(sourcePath) && File.Exists(destPath))
                    {
                        if (File.GetLastWriteTimeUtc(sourcePath) > File.GetLastWriteTimeUtc(destPath))
                        {
                            File.Copy(sourcePath, destPath, true);
                            Console.WriteLine($"Fichier source synchronisé : {sourcePath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erreur de synchronisation ({sourcePath}) : {ex.Message}");
                }
            }

            // parcours de chaque fichier dans le dossier import ordonné par date d'écriture
            var files = Directory.GetFiles(importsDir)
                .OrderBy(File.GetLastWriteTimeUtc);

            foreach (var file in files)
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

        // tri des données pour enlever les doublons 
        var combinedValues = allData
            .GroupBy(w => new { w.DateData, w.CityName })
            .Select(g => g.Last())
            .OrderBy(w => w.DateData)
            .ToList();

        // création de la serie de données à partir des valeurs sans doublons
        _series = DataSerie<Weather>.From(combinedValues);
        
        Console.WriteLine($"Données rechargées. Nombre d'éléments : {_series.Count}");
        
        RefreshPlot();
    }

    private void RefreshButton_Click(object? sender, RoutedEventArgs e)
    {
        ReloadData();
    }



    /// <summary>
    /// mets a jour tout le graphique et re créé les checkboxes pour chaque villes
    /// </summary>
    private void RefreshPlot()
    {
        // Sauvegarder l'état des cases à cocher existantes pour conserver la sélection de l'utilisateur
        var previousCheckedStates = CityCheckBoxContainer.Children
            .OfType<CheckBox>()
            .ToDictionary(cb => cb.Content?.ToString() ?? "", cb => cb.IsChecked == true);

        // nettoyage de l'ancien affichage
        MyPlot.Plot.Clear();
        CityCheckBoxContainer.Children.Clear();

        if (_series == null || !_series.Values.Any())
        {
            MyPlot.Refresh();
            return;
        }

        // regroupement des données par villes
        var cityPlotsData = _series.Values
            .GroupBy(dp => dp.CityName)
            .Select(group => (
                CityName: group.Key,
                Dates: group.Select(dp => dp.DateData.ToOADate()).ToArray(),
                Temps: group.Select(dp => dp.Temperature).ToArray()
            ))
            .ToList();

        // génération des courbes et de leurs checkBoxes
        var checkBoxes = cityPlotsData.Select(data =>
        {
            var scatter = MyPlot.Plot.Add.Scatter(data.Dates, data.Temps);
            scatter.LegendText = data.CityName;

            bool isChecked = !previousCheckedStates.TryGetValue(data.CityName, out bool wasChecked) || wasChecked;
            scatter.IsVisible = isChecked;

            var checkBox = new CheckBox
            {
                Content = data.CityName,
                IsChecked = isChecked,
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

        foreach (var file in files)
        {
            try
            {
                string sourcePath = file.Path.LocalPath;
                if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) continue;

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string destPath = Path.Combine(importsDir, $"{timestamp}_{Path.GetFileName(sourcePath)}");

                File.Copy(sourcePath, destPath, true);
                _importedSourceFiles[sourcePath] = destPath;
                Console.WriteLine($"Fichier copié : {destPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur ({file.Name}) : {ex.Message}");
            }
        }

        ReloadData();
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
                    .Select(w => $"{w.DateData:yyyy-MM-dd HH:mm:ss},{w.CityName},{w.Temperature.ToString(CultureInfo.InvariantCulture)}")
                    .Prepend("Date,Ville,Temperature");

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