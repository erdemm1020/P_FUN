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
    /// Recharge les données depuis les fichiers et met à jour toute l'application
    /// </summary>
    private void ReloadData()
    {
        string importsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FichierIMport");
        List<Weather> allData = new List<Weather>();

        if (Directory.Exists(importsDir))
        {
            // synchronise les fichiers sources qui ont été modifiés
            _importedSourceFiles.ToList().ForEach(SyncFile);

            // charge tous les fichiers .csv, du plus ancien au plus récent
            allData = Directory.GetFiles(importsDir)
                .Where(f => Path.GetExtension(f).Equals(".csv", StringComparison.OrdinalIgnoreCase))
                .OrderBy(File.GetLastWriteTimeUtc)
                .SelectMany(LoadFile)
                .ToList();
        }

        // enlève les doublons (on garde la donnée la plus récente) et trie par date
        List<Weather> combinedValues = allData
            .GroupBy(w => new { w.DateData, w.CityName })
            .Select(g => g.Last())
            .OrderBy(w => w.DateData)
            .ToList();

        // crée la série de données sans doublons
        _series = DataSerie<Weather>.From(combinedValues);

        Console.WriteLine($"Données rechargées. Nombre d'éléments : {_series.Count}");

        RefreshPlot();
    }

    /// <summary>
    /// Recopie le fichier source dans le dossier d'import s'il est plus récent
    /// </summary>
    private static void SyncFile(KeyValuePair<string, string> f)
    {
        try
        {
            // copie seulement si les deux fichiers existent et que la source est plus récente
            if (File.Exists(f.Key) && File.Exists(f.Value)
                && File.GetLastWriteTimeUtc(f.Key) > File.GetLastWriteTimeUtc(f.Value))
            {
                File.Copy(f.Key, f.Value, true);
                Console.WriteLine($"Fichier source synchronisé : {f.Key}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur de synchronisation ({f.Key}) : {ex.Message}");
        }
    }

    /// <summary>
    /// Lit un fichier CSV et renvoie ses données (vide en cas d'erreur)
    /// </summary>
    private static IEnumerable<Weather> LoadFile(string file)
    {
        try
        {
            // ToList() force la lecture ici, pour que l'erreur soit attrapée par le try
            return DataSerie<Weather>.FromCsv(file, Parser.ParseWeather).Values.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur de chargement ({file}) : {ex.Message}");
            return Enumerable.Empty<Weather>();
        }
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
        Dictionary<string, bool> previousCheckedStates = CityCheckBoxContainer.Children
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

        // ouvre la fenêtre de sélection de fichiers
        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Sélectionner un fichier",
            AllowMultiple = true,
            FileTypeFilter = new[] { new FilePickerFileType("Fichiers CSV") { Patterns = new[] { "*.csv" } } }
        });

        // aucun fichier choisi
        if (files.Count == 0) return;

        // crée le dossier d'import s'il n'existe pas
        string importsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FichierIMport");
        Directory.CreateDirectory(importsDir);

        // récupère les chemins des fichiers puis copie chacun dans le dossier d'import
        files
            .Select(file => file.Path.LocalPath)
            .ToList()
            .ForEach(path => CopyToImports(path, importsDir));

        ReloadData();
    }

    /// <summary>
    /// Copie un fichier dans le dossier d'import et garde le lien source et la copie
    /// </summary>
    private void CopyToImports(string sourcePath, string importsDir)
    {
        try
        {
            // ignore les fichiers introuvables
            if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath)) return;

            // ajoute la date et l'heure devant le nom pour éviter les doublons
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string destPath = Path.Combine(importsDir, $"{timestamp}_{Path.GetFileName(sourcePath)}");

            File.Copy(sourcePath, destPath, true);
            _importedSourceFiles[sourcePath] = destPath;
            Console.WriteLine($"Fichier copié : {destPath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur ({sourcePath}) : {ex.Message}");
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
                MyPlot.Plot.SavePng(pngPath, (int)MyPlot.Bounds.Width, (int)MyPlot.Bounds.Height);
                Console.WriteLine($"Export PNG : {pngPath}");
                break;

            case "CSV":
                string csvPath = Path.Combine(exportsDir, $"{timestamp}_donnees_export.csv");
                var csvLines = _series.Values
                    .Select(w =>
                        $"{w.DateData:yyyy-MM-dd HH:mm:ss},{w.CityName},{w.Temperature.ToString(CultureInfo.InvariantCulture)}")
                    .Prepend("Date,Ville,Temperature");

                File.WriteAllLines(csvPath, csvLines);
                Console.WriteLine($"Export CSV : {csvPath}");
                break;
        }
    }

    /// <summary>
    /// Zoom avant dans le graphique 
    /// </summary>
    private void ZoomInButton_Click(object? sender, RoutedEventArgs e)
    {
        MyPlot.Plot.Axes.Zoom(1.25, 1.25);
        MyPlot.Refresh();
    }

    /// <summary>
    /// Zoom arrière dans le graphique
    /// </summary>
    private void ZoomOutButton_Click(object? sender, RoutedEventArgs e)
    {
        MyPlot.Plot.Axes.Zoom(0.8, 0.8);
        MyPlot.Refresh();
    }

    /// <summary>
    /// Re adapte la vue pour afficher l'ensemble des données
    /// </summary>
    private void ResetViewButton_Click(object? sender, RoutedEventArgs e)
    {
        MyPlot.Plot.Axes.AutoScale();
        MyPlot.Refresh();
    }

    /// <summary>
    /// Ajoute manuellement de données
    /// </summary>
    private void AddDataButton_Click(object? sender, RoutedEventArgs e)
    {
        string cityName = CityInputTextBox.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(cityName))
        {
            Console.WriteLine("Erreur : Le nom de la ville est invalide");
            return;
        }

        if (!double.TryParse(TemperatureInputTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double temperature))
        {
            Console.WriteLine("Erreur : La température doit être un nombre valide.");
            return;
        }

        DateTime selectedDate = DateInputPicker.SelectedDate?.DateTime ?? DateTime.Now;

        string importsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FichierIMport");
        Directory.CreateDirectory(importsDir);

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string manualFilePath = Path.Combine(importsDir, $"{timestamp}_manuelle.csv");

        string csvHeader = "Date,Ville,Temperature";
        string csvLine =
            $"{selectedDate:yyyy-MM-dd HH:mm:ss},{cityName},{temperature.ToString(CultureInfo.InvariantCulture)}";

        try
        {
            File.WriteAllLines(manualFilePath, new[] { csvHeader, csvLine });
            Console.WriteLine($"Nouvelle donnée ajoutée dans : {manualFilePath}");

            CityInputTextBox.Text = string.Empty;
            TemperatureInputTextBox.Text = string.Empty;

            ReloadData();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'enregistrement de la donnée : {ex.Message}");
        }
    }
}