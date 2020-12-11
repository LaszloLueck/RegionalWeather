#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Optional;
using Optional.Linq;
using Optional.Unsafe;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Filestorage;
using RegionalWeather.Logging;
using RegionalWeather.Owm;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

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
                var dataReader = new OwmApiReader();
                
                var locationsWeather = locations.Select(location =>
                {
                    return dataReader.ReadDataFromLocation(location, configuration.OwmApiKey)
                        .Select(locationData => storageImpl.WriteData(locationData))
                        .Flatten()
                        .Map(locationData => JsonSerializer.Deserialize<Root>(locationData))
                        .Where(element => element != null)
                        .Select(element => element!)
                        .Select(OwmToElasticDocumentConverter.Convert)
                        .ValueOrFailure();
                });


                elasticConnection.BulkWriteDocument(locationsWeather, configuration.ElasticIndexName);
                
                storageImpl.FlushData();
                storageImpl.CloseFileStream();
            });
        }
    }
}