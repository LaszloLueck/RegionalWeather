using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Logging;
using RegionalWeather.Owm;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;
using Clouds = RegionalWeather.Elastic.Clouds;
using Wind = RegionalWeather.Elastic.Wind;

namespace RegionalWeather.Scheduler
{
    public class SchedulerJob : IJob
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<SchedulerJob>.GetLogger();

        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            var locations = (List<string>) context.JobDetail.JobDataMap["locations"];
            await Task.Run(async () =>
            {
                await Log.InfoAsync("Use the following parameter for connections:");
                await Log.InfoAsync($"Parallelism: {configuration.Parallelism}");
                await Log.InfoAsync($"Runs every: {configuration.RunsEvery} s");
                await Log.InfoAsync($"Path to Locations file: {configuration.PathToLocationsMap}");
                await Log.InfoAsync($"ElasticSearch: {configuration.ElasticHostsAndPorts}");
                IElasticConnectionBuilder connectionBuilder =
                    new ElasticConnectionBuilder();
                var elasticConnection = connectionBuilder.Build(configuration);

                if (!elasticConnection.IndexExists(configuration.ElasticIndexName))
                {
                    if (elasticConnection.CreateIndex(configuration.ElasticIndexName))
                    {
                        await Log.InfoAsync($"index {configuration.ElasticIndexName} successfully created");
                    }
                    else
                    {
                        await Log.WarningAsync($"error while create index {configuration.ElasticIndexName}");
                    }
                }
                else
                {
                    //elasticConnection.DeleteIndex(configuration.ElasticIndexName);
                }

                IFileStorage fileStorage = new FileStorage();
                var storageImpl = fileStorage.Build(configuration);
                
                foreach (var location in locations)
                {
                    new OwmApiReader().ReadDataFromLocation(location, configuration.OwmApiKey
                    ).MatchSome(result =>
                    {
                        var res = JsonSerializer.Deserialize<Root>(result);
                        storageImpl.WriteData(result);

                        if (res == null) return;
                        
                        var etl = new WeatherLocationDocument();

                        var clds = new Clouds
                        {
                            Density = res.Clouds.All,
                            Description = res.Weather[0].Description,
                            Visibility = res.Visibility,
                            CloudType = res.Weather[0].Main
                        };
                        etl.Clouds = clds;

                        var loc = new Location
                        {
                            Longitude = Math.Round(res.Coord.Lon, 2), Latitude = Math.Round(res.Coord.Lat, 2)
                        };
                        etl.Location = loc;

                        etl.TimeStamp = DateTime.Now;
                        etl.DateTime = DateTimeOffset.FromUnixTimeSeconds(res.Dt).LocalDateTime;
                        etl.Sunrise = DateTimeOffset.FromUnixTimeSeconds(res.Sys.Sunrise).LocalDateTime;
                        etl.SunSet = DateTimeOffset.FromUnixTimeSeconds(res.Sys.Sunset).LocalDateTime;

                        var tmp = new Temperatures
                        {
                            Humidity = res.Main.Humidity,
                            Pressure = res.Main.Pressure,
                            Temperature = Math.Round(res.Main.Temp, 2),
                            FeelsLike = Math.Round(res.Main.FeelsLike, 2),
                            TemperatureMax = Math.Round(res.Main.TempMax, 2),
                            TemperatureMin = Math.Round(res.Main.TempMin, 2)
                        };
                        etl.Temperatures = tmp;

                        var wnd = new Wind {Direction = res.Wind.Deg, Speed = Math.Round(res.Wind.Speed, 2), Gust = Math.Round(res.Wind.Gust, 2)};
                        etl.Wind = wnd;

                        etl.LocationId = res.Id;
                        etl.LocationName = res.Name;
                        elasticConnection.WriteDocument(etl, configuration.ElasticIndexName);
                    });
                }
                
                storageImpl.FlushData();
                storageImpl.CloseFileStream();
                
            });
        }
    }
}