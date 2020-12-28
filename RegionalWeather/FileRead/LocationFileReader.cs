using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;

namespace RegionalWeather.FileRead
{
    public interface ILocationFileReader
    {
        ILocationFileReaderImpl Build(ConfigurationItems configurationItems);
    }

    public class LocationFileReader : ILocationFileReader
    {


        public ILocationFileReaderImpl Build(ConfigurationItems configurationItems)
        {
            return new LocationFileReaderImpl(configurationItems);
        }
    }

    public interface ILocationFileReaderImpl
    {
        public Task<Option<List<string>>> ReadConfigurationAsync();
    }
    
    public class LocationFileReaderImpl : ILocationFileReaderImpl
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<LocationFileReaderImpl>.GetLogger();
        private readonly ConfigurationItems _configurationItems;
        public LocationFileReaderImpl(ConfigurationItems configurationItems)
        {
            _configurationItems = configurationItems;
        }

        public async Task<Option<List<string>>> ReadConfigurationAsync()
        {
            await Log.InfoAsync("Try to read the list of locations");
            try
            {
                return await Task.Run(async () =>
                {
                    var sr = new StreamReader(_configurationItems.PathToLocationsMap);
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
                await Log.ErrorAsync(exception, "An error occured");
                return await Task.Run(Option.None<List<string>>);
            }
        }
    }
}