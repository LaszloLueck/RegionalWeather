using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Optional;
using Serilog;

namespace RegionalWeather.Transport.Owm
{

    public interface IOwmApiReader
    {
        public Task<Option<string>> ReadDataFromLocationAsync(string url, string location);
    }
    
    
    public class OwmApiReader : IOwmApiReader
    {
        private readonly ILogger _logger;

        public OwmApiReader()
        {
            _logger = Log.Logger.ForContext<OwmApiReader>();
        }

        public async Task<Option<string>> ReadDataFromLocationAsync(string url, string location)
        {
            var sw = Stopwatch.StartNew();

            try
            {
                return Option.Some(await new WebClient().DownloadStringTaskAsync(url));
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Error while getting weather information for url {url}");
                return await Task.Run(Option.None<string>);
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms for {Location}", "ReadDataFromLocationAsync",
                    sw.ElapsedMilliseconds, location);
            }
        } 
        
    }
}