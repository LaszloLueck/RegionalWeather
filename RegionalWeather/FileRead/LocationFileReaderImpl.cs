using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Optional;
using Serilog;

namespace RegionalWeather.FileRead
{
    public interface ILocationFileReader
    {
        public Task<Option<List<string>>> ReadLocationsAsync(string locationPath);
    }
    
    public class LocationFileReaderImpl : ILocationFileReader
    {
        private readonly ILogger _logger;
        
        public LocationFileReaderImpl(ILogger loggingBase)
        {
            _logger = loggingBase.ForContext<LocationFileReaderImpl>();
        }

        public async Task<Option<List<string>>> ReadLocationsAsync(string locationPath)
        {
            _logger.Information("Try to read the list of locations");
            try
            {
                return await Task.Run(async () =>
                {
                    using var sr = new StreamReader(locationPath);
                    string line;
                    var l = new List<string>();
                    while ((line = await sr.ReadLineAsync()) != null)
                    {
                        l.Add(line);
                    }

                    return Option.Some(l);
                });
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "An error occured");
                return await Task.Run(Option.None<List<string>>);
            }
        }
    }
}