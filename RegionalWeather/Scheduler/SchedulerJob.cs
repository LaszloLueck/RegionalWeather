#nullable enable
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Optional.Collections;
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

                var locationList =
                    (await new LocationFileReader().Build(configuration).ReadConfigurationAsync()).ValueOr(
                        new List<string>());

                var rootTasksOption = locationList.Select(async location =>
                    await OwmApiReader.ReadDataFromLocationAsync(location, configuration.OwmApiKey));

                var rootStrings = (await Task.WhenAll(rootTasksOption)).Values();
                var toElastic = (await Task.WhenAll(rootStrings.Select(async str => await DeserializeObjectAsync(str))))
                    .Where(item => item != null)
                    .Select(itemNullable =>
                    {
                        var item = itemNullable!;
                        item.ReadTime = DateTime.Now;
                        return item!;
                    });
                var concurrentBag = new ConcurrentBag<Root>(toElastic);
                await storageImpl.WriteAllDataAsync(concurrentBag);
                var elasticDocs =
                    await Task.WhenAll(concurrentBag.Select(async root =>
                        await OwmToElasticDocumentConverter.ConvertAsync(root)));
                await elasticConnection.BulkWriteDocumentsAsync(elasticDocs, configuration.ElasticIndexName);
            });
        }

        private static async ValueTask<Root?> DeserializeObjectAsync(string data)
        {
            await using MemoryStream stream = new();
            var bt = Encoding.UTF8.GetBytes(data);
            await stream.WriteAsync(bt.AsMemory(0, bt.Length));
            stream.Position = 0;
            return await JsonSerializer.DeserializeAsync<Root>(stream);
        }
    }
}