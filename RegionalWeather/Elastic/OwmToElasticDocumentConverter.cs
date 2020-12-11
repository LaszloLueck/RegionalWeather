using System;
using Nest;
using RegionalWeather.Owm;

namespace RegionalWeather.Elastic
{
    public static class OwmToElasticDocumentConverter
    {
        public static WeatherLocationDocument Convert(Root owmDoc)
        {
            var etl = new WeatherLocationDocument();

            var clds = new Clouds
            {
                Density = owmDoc.Clouds.All,
                Description = owmDoc.Weather[0].Description,
                Visibility = owmDoc.Visibility,
                CloudType = owmDoc.Weather[0].Main
            };
            etl.Clouds = clds;

            var loc = new Location
            {
                Longitude = Math.Round(owmDoc.Coord.Lon, 2), Latitude = Math.Round(owmDoc.Coord.Lat, 2)
            };
            etl.Location = loc;

            etl.TimeStamp = DateTime.Now;
            etl.DateTime = DateTimeOffset.FromUnixTimeSeconds(owmDoc.Dt).LocalDateTime;
            etl.Sunrise = DateTimeOffset.FromUnixTimeSeconds(owmDoc.Sys.Sunrise).LocalDateTime;
            etl.SunSet = DateTimeOffset.FromUnixTimeSeconds(owmDoc.Sys.Sunset).LocalDateTime;

            var tmp = new Temperatures
            {
                Humidity = owmDoc.Main.Humidity,
                Pressure = owmDoc.Main.Pressure,
                Temperature = Math.Round(owmDoc.Main.Temp, 2),
                FeelsLike = Math.Round(owmDoc.Main.FeelsLike, 2),
                TemperatureMax = Math.Round(owmDoc.Main.TempMax, 2),
                TemperatureMin = Math.Round(owmDoc.Main.TempMin, 2)
            };
            etl.Temperatures = tmp;

            var wnd = new Wind {Direction = owmDoc.Wind.Deg, Speed = Math.Round(owmDoc.Wind.Speed, 2)};
            etl.Wind = wnd;

            etl.LocationId = owmDoc.Id;
            etl.LocationName = owmDoc.Name;
            etl.GeoLocation = new GeoLocation(owmDoc.Coord.Lat, owmDoc.Coord.Lon);
            return etl;
        }
        
        
    }
}