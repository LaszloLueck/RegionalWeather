using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Logging;

namespace RegionalWeather.FileRead
{
    public interface ILocationFileReader
    {
        ILocationFileReaderImpl Build();
    }

    public class LocationFileReader : ILocationFileReader
    {


        public ILocationFileReaderImpl Build()
        {
            return new LocationFileReaderImpl();
        }
    }

    public interface ILocationFileReaderImpl
    {
        public Task<Option<List<string>>> ReadLocationsAsync(string locationPath);
    }
    
    public class LocationFileReaderImpl : ILocationFileReaderImpl
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<LocationFileReaderImpl>.GetLogger();

        public LocationFileReaderImpl()
        {

        }

        public async Task<Option<List<string>>> ReadLocationsAsync(string locationPath)
        {
            await Log.InfoAsync("Try to read the list of locations");
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
                await Log.ErrorAsync(exception, "An error occured");
                return await Task.Run(Option.None<List<string>>);
            }
        }
    }
}