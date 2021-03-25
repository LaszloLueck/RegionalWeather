using System;
using System.Net;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Logging;
using Serilog;

namespace RegionalWeather.Transport.Owm
{

    public interface IOwmApiReader
    {
        public Task<Option<string>> ReadDataFromLocationAsync(string url);
    }
    
    
    public class OwmApiReader : IOwmApiReader
    {
        private readonly ILogger _logger;

        public OwmApiReader(ILogger loggingBase)
        {
            _logger = loggingBase.ForContext<OwmApiReader>();
        }

        public async Task<Option<string>> ReadDataFromLocationAsync(string url)
        {
            try
            {
                return Option.Some(await new WebClient().DownloadStringTaskAsync(url));
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Error while getting weather information for url {url}");
                return await Task.Run(Option.None<string>);
            }
        } 
        
    }
}