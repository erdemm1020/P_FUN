using System;
using System.Globalization;
using System.IO;
using DataSeries;

namespace FUN;

public static class Extensions
{
    /// <summary>
    /// Indique si le chemin correspond à un fichier .csv
    /// </summary>
    public static bool IsCsv(this string path)
    {
        return Path.GetExtension(path).Equals(".csv", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Convertit une donnée météo en ligne CSV (Date,Ville,Temperature)
    /// </summary>
    public static string ToCsvLine(this Weather w)
    {
        return $"{w.DateData:yyyy-MM-dd HH:mm:ss},{w.CityName},{w.Temperature.ToString(CultureInfo.InvariantCulture)}";
    }
}