using System;
using System.Threading.Tasks;
using Nest;
using Optional;
using RegionalWeather.Logging;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Owm.CurrentWeather;

namespace RegionalWeather.Elastic
{
    public interface IOwmToElasticDocumentConverter<in T>
    {
        public Task<Option<ElasticDocument>> ConvertAsync(T owmDoc);
    }

    public class AirPollutionToElasticDocumentConverter : IOwmToElasticDocumentConverter<AirPollutionBase>
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<AirPollutionToElasticDocumentConverter>
            .GetLogger();
        
        public async Task<Option<ElasticDocument>> ConvertAsync(AirPollutionBase owmDoc)
        {
            try
            { 
                var result = await Task.Run(() =>
                {
                    var ret = new AirPollutionDocument {Id = Guid.NewGuid(), TimeStamp = owmDoc.ReadTime};
                    var lstElement = owmDoc.List[0];
                    ret.DateTime = DateTimeOffset.FromUnixTimeSeconds(lstElement.Dt).LocalDateTime;
                    ret.AirQualityIndex = lstElement.Main.Aqi;
                    ret.GeoLocation = new GeoLocation(owmDoc.Coord.Lat, owmDoc.Coord.Lon);
                    ret.LocationName = owmDoc.LocationName;
                    ret.Co = Math.Round(lstElement.Components.Co, 2);
                    ret.Nh3 = Math.Round(lstElement.Components.Nh3, 2);
                    ret.No = Math.Round(lstElement.Components.No, 2);
                    ret.No2 = Math.Round(lstElement.Components.No2, 2);
                    ret.O3 = Math.Round(lstElement.Components.O3, 2);
                    ret.Pm10 = Math.Round(lstElement.Components.Pm10, 2);
                    ret.Pm25 = Math.Round(lstElement.Components.Pm25, 2);
                    ret.So2 = Math.Round(lstElement.Components.So2, 2);

                    return ret;
                });
                return Option.Some((ElasticDocument) result);
            }
            catch (Exception exception)
            {
                await Log.ErrorAsync(exception, "Error while converting to Elastic Document");
                return Option.None<ElasticDocument>();
            }
        }
    }
    
    
    public class OwmToElasticDocumentConverter : IOwmToElasticDocumentConverter<CurrentWeatherBase>
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<OwmToElasticDocumentConverter>.GetLogger();
        
        public async Task<Option<ElasticDocument>> ConvertAsync(CurrentWeatherBase owmDoc)
        {
            try
            {
                var result = await Task.Run(() => Convert(owmDoc));
                return Option.Some((ElasticDocument)result);
            }
            catch (Exception exception)
            {
                await Log.ErrorAsync(exception, "Error while converting to Elastic Document");
                return Option.None<ElasticDocument>();
            }
        }

        private static WeatherLocationDocument Convert(CurrentWeatherBase owmDoc)
        {
            var etl = new WeatherLocationDocument {Id = Guid.NewGuid()};


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

            etl.TimeStamp = owmDoc.ReadTime;
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

            var wnd = new Wind
            {
                Direction = owmDoc.Wind.Deg, 
                Speed = Math.Round(owmDoc.Wind.Speed, 2),
                Gust = Math.Round(owmDoc.Wind.Gust, 2)
            };
            etl.Wind = wnd;



            var rain = new Rain();


            if (owmDoc.Rain != null)
            {
                rain.OneHour = owmDoc.Rain.OneHour;
                rain.ThreeHour = owmDoc.Rain.ThreeHour;
            }
            else
            {
                rain.OneHour = 0;
                rain.ThreeHour = 0;
            }
            
            etl.Rain = rain;

            var snow = new Snow();
            if (owmDoc.Snow != null)
            {
                snow.OneHour = owmDoc.Snow.OneHour;
                snow.ThreeHour = owmDoc.Snow.ThreeHour;
            }
            else
            {
                snow.OneHour = 0;
                snow.ThreeHour = 0;
            }

            etl.Snow = snow;
            
            etl.LocationId = owmDoc.Id;
            etl.LocationName = owmDoc.Name;
            etl.GeoLocation = new GeoLocation(owmDoc.Coord.Lat, owmDoc.Coord.Lon);
            return etl;
        }
    }
}