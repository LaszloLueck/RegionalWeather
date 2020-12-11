using System;
using System.Net;
using Optional;
using RegionalWeather.Logging;

namespace RegionalWeather.Transport.Owm
{
    public class OwmApiReader
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<OwmApiReader>.GetLogger();
        public Option<string> ReadDataFromLocation(string location, string apiKey)
        {
            var url = $"https://api.openweathermap.org/data/2.5/weather?{location}&APPID={apiKey}&units=metric";
            try
            {
                var result = new WebClient().DownloadString(url);
                return Option.Some(result);
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"Error while getting weather information from location {location}");
                return Option.None<string>();
            }
        }
    }
}