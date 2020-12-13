using System;
using System.Net;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Logging;

namespace RegionalWeather.Transport.Owm
{
    public class OwmApiReader
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<OwmApiReader>.GetLogger();
        private static readonly string Url = "https://api.openweathermap.org/data/2.5/weather?{0}&APPID={1}&units=metric";
        
        public static Option<string> ReadDataFromLocation(string location, string apiKey)
        {
            try
            {
                var result = new WebClient().DownloadString(String.Format(Url, location, apiKey));
                return Option.Some(result);
            }
            catch (Exception exception)
            {
                Log.Error(exception, $"Error while getting weather information from location {location}");
                return Option.None<string>();
            }
        }

        public static async Task<Option<string>> ReadDataFromLocationAsync(string location, string apiKey)
        {
            try
            {
                return Option.Some(await new WebClient().DownloadStringTaskAsync(String.Format(Url, location, apiKey)));
            }
            catch (Exception exception)
            {
                await Log.ErrorAsync(exception, $"Error while getting weather information from location {location}");
                return await Task.Run(Option.None<string>);
            }
        } 
        
    }
}