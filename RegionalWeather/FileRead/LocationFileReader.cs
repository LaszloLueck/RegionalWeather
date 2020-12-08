using System;
using System.Collections.Generic;
using System.IO;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;

namespace RegionalWeather.FileRead
{
    interface ILocationFileReader
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

        public Option<List<string>> ReadConfiguration()
        {
            Log.Info("Try to read the list of locations");
            try
            {
                var sr = new StreamReader(_configurationItems.PathToLocationsMap);
                string line = "";
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