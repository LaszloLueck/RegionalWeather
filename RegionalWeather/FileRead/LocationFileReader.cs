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
        LocationFileReaderImpl Build(ConfigurationItems configurationItems);
    }

    public class LocationFileReader : ILocationFileReader
    {


        public LocationFileReaderImpl Build(ConfigurationItems configurationItems)
        {
            return new(configurationItems);
        }
    }
    
    public class LocationFileReaderImpl
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
        
        public Option<List<string>> ReadConfiguration()
        {
            Log.Info("Try to read the list of locations");
            try
            {
                var sr = new StreamReader(_configurationItems.PathToLocationsMap);
                string line;
                var l = new List<string>();
                while ((line = sr.ReadLine()) != null)
                {
                    l.Add(line);
                }

                return Option.Some(l);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "An error occured");
                return Option.None<List<string>>();
            }

        }
        
    }
}