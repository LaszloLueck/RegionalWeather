#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Filestorage;
using RegionalWeather.Logging;
using RegionalWeather.Owm;
using RegionalWeather.Transport.Elastic;

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

                var locationsWeather = (from location in locations select location)
                    .Select(toWrite =>
                    {
                        storageImpl.WriteData(toWrite);
                        return toWrite;
                    })
                    .Select(loc => JsonSerializer.Deserialize<Root>(loc))
                    .Where(element => element != null)
                    .Select(element => element!)
                    .Select(element =>
                    {
                        Log.Info("Prepare doc: " + element.Name);
                        return element;
                    })
                    .Select(OwmToElasticDocumentConverter.Convert);

                elasticConnection.BulkWriteDocument(locationsWeather, configuration.ElasticIndexName);
                
                storageImpl.FlushData();
                storageImpl.CloseFileStream();
            });
        }
    }
}