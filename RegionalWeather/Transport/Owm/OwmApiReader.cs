using System;
using System.Net;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Logging;

namespace RegionalWeather.Transport.Owm
{

    public interface IOwmApiReader
    {
        public Task<Option<string>> ReadDataFromLocationAsync(string url);
    }
    
    
    public class OwmApiReader : IOwmApiReader
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<OwmApiReader>.GetLogger();

        public async Task<Option<string>> ReadDataFromLocationAsync(string url)
        {
            try
            {
                return Option.Some(await new WebClient().DownloadStringTaskAsync(url));
            }
            catch (Exception exception)
            {
                await Log.ErrorAsync(exception, $"Error while getting weather information for url {url}");
                return await Task.Run(Option.None<string>);
            }
        } 
        
    }
}