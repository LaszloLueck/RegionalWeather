#nullable enable
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Logging;
using RegionalWeather.Processing;
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
                var locationReader = new LocationFileReader().Build(configuration);
                var owmReader = new OwmApiReader();

                var owmConverter = new OwmToElasticDocumentConverter();

                var processor =
                    new ProcessingBaseCurrentWeatherImpl(elasticConnection, locationReader, owmReader, storageImpl,
                        owmConverter);

                var elasticIndexSuccess = true;
                if (!await processor.ElasticIndexExistsAsync(configuration.ElasticIndexName))
                {
                    elasticIndexSuccess = await processor.CreateIndexAsync(configuration.ElasticIndexName);
                }

                if (elasticIndexSuccess)
                {
                    await processor.Process(configuration);
                }
            });
        }
    }
}