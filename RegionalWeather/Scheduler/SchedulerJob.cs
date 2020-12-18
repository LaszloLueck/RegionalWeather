#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Optional.Linq;
using Optional.Unsafe;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
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
            IFileStorage fileStorage = new FileStorage();
            var storageImpl = fileStorage.Build(configuration);
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

                if (!await elasticConnection.IndexExistsAsync(configuration.ElasticIndexName))
                {
                    if (await elasticConnection.CreateIndexAsync(configuration.ElasticIndexName))
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

                var lo =
                    (await new LocationFileReader().Build(configuration).ReadConfigurationAsync()).ValueOr(
                        new List<string>());
                var rootTasks = lo.Select(async location =>
                {
                    var root = (await OwmApiReader.ReadDataFromLocationAsync(location, configuration.OwmApiKey))
                        .Select(data => JsonSerializer.Deserialize<Root>(data))
                        .Where(element => element != null)
                        .Select(element => element!)
                        .Select(element =>
                        {
                            element.ReadTime = DateTime.Now;
                            return element;
                        })
                        .ValueOrFailure();

                    return root;
                });

                var toElastic = await Task.WhenAll(rootTasks);
                await storageImpl.WriteAllDataAsync(toElastic);
                var elasticDocs =
                    await Task.WhenAll(toElastic.Select(async root => await OwmToElasticDocumentConverter.ConvertAsync(root)));
                await elasticConnection.BulkWriteDocumentsAsync(elasticDocs, configuration.ElasticIndexName);
            });
        }
    }
}