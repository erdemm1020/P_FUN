using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataSeries;
using FUN;
using Xunit;

namespace FUN.Tests;

/// <summary>
/// Tests de la lecture des fichiers CSV
/// </summary>
public class LectureCsvTests
{
    /// <summary>
    /// Crée un fichier CSV temporaire avec l'en-tête et les lignes données
    /// </summary>
    private static string CreateCsv(params string[] lignes)
    {
        string chemin = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".csv");
        File.WriteAllLines(chemin, lignes.Prepend("Date,Ville,Temperature"));
        return chemin;
    }

    /// <summary>
    /// Une température négative est bien lue (hiver en Turquie)
    /// </summary>
    [Fact]
    public void FromCsv_TemperatureNegative_LueCorrectement()
    {
        string chemin = CreateCsv("2026-01-15 00:00:00,Ankara,-5.5");

        List<Weather> donnees = DataSerie<Weather>.FromCsv(chemin, Parser.ParseWeather).Values.ToList();
        File.Delete(chemin);

        Assert.Equal(-5.5, donnees[0].Temperature);
    }

    /// <summary>
    /// Les 5 villes du projet sont bien reconnues comme 5 villes différentes
    /// </summary>
    [Fact]
    public void FromCsv_CinqVilles_CinqSeriesDifferentes()
    {
        string chemin = CreateCsv(
            "2026-10-01 00:00:00,Istanbul,18",
            "2026-10-01 00:00:00,Ankara,12",
            "2026-10-01 00:00:00,Antalya,24",
            "2026-10-01 00:00:00,Gaziantep,20",
            "2026-10-01 00:00:00,Kahramanmaras,19");

        List<Weather> donnees = DataSerie<Weather>.FromCsv(chemin, Parser.ParseWeather).Values.ToList();
        File.Delete(chemin);

        int nombreVilles = donnees.Select(meteo => meteo.CityName).Distinct().Count();
        Assert.Equal(5, nombreVilles);
    }

    /// <summary>
    /// Un fichier avec seulement l'en-tête ne contient aucune donnée
    /// </summary>
    [Fact]
    public void FromCsv_SeulementEnTete_SerieVide()
    {
        string chemin = CreateCsv();

        DataSerie<Weather> serie = DataSerie<Weather>.FromCsv(chemin, Parser.ParseWeather);
        File.Delete(chemin);

        Assert.Equal(0, serie.Count);
    }
}