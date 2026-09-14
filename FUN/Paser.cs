using System;
using System.Globalization;
using FUN;

namespace DataSeries;

public class Parser
{
    public static DataPoint<Weather> ParseWeather(string[] cols)
    {
        if (cols == null || cols.Length < 3)
        {
            throw new ArgumentException("La ligne doit contenir au moins 3 colonnes (Date, Ville, Température).", nameof(cols));
        }   

        DateTime date = DateTime.Parse(cols[0], CultureInfo.InvariantCulture);
        string cityName = cols[1];
        double temperature = double.Parse(cols[2], CultureInfo.InvariantCulture);
        double degres = cols.Length > 3
            ? double.Parse(cols[3], CultureInfo.InvariantCulture)
            : 0;

        Weather weather = new(date, cityName, temperature, degres);
        return new DataPoint<Weather>(date, weather);
    }
}