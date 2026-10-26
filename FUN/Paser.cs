using System;
using System.Globalization;
using FUN;

namespace DataSeries;

public class Parser
{
    public static Weather ParseWeather(string[] cols)
    {
        if (cols == null || cols.Length < 3)
        {
            throw new ArgumentException("La ligne doit contenir au moins 3 colonnes (Date, Ville, Température).", nameof(cols));
        }   

        DateTime date = DateTime.Parse(cols[0].Trim(), CultureInfo.InvariantCulture);
        string cityName = cols[1].Trim();
        double temperature = double.Parse(cols[2].Trim(), CultureInfo.InvariantCulture);


        return new Weather(date, cityName, temperature);
    }
}