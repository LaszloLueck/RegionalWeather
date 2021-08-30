using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        
        public LocationFileReaderImpl()
        {
            _logger = Log.Logger.ForContext<LocationFileReaderImpl>();
        }

        public async Task<Option<List<string>>> ReadLocationsAsync(string locationPath)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                _logger.Information("Try to read the list of locations");
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
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "ReadLocationsAsync",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}