using System;

namespace FUN;

public class Weather
{
    public string CityName { get; }
    public DateTime DateData { get; }
    public double Temperature { get; }
    public double Degres { get; }

    public Weather(DateTime dateData, string cityName, double temperature, double degres = 0)
    {
        DateData = dateData;
        CityName = cityName;
        Temperature = temperature;
        Degres = degres;
    }
}