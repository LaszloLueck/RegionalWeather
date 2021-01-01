#nullable enable
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Logging;
using RegionalWeather.Owm;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Processing;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Scheduler
{
    public class CurrentWeatherSchedulerJob : IJob
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<CurrentWeatherSchedulerJob>.GetLogger();

        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            await Task.Run(async () =>
            {
                await Log.InfoAsync("Use the following parameters for this job:");
                await Log.InfoAsync($"Parallelism: {configuration.Parallelism}");
                await Log.InfoAsync($"Runs every: {configuration.RunsEvery} s");
                await Log.InfoAsync($"Path to Locations file: {configuration.PathToLocationsMap}");
                await Log.InfoAsync($"Write to Elastic index: {configuration.ElasticIndexName}");
                await Log.InfoAsync($"ElasticSearch: {configuration.ElasticHostsAndPorts}");
                
                IFileStorage fileStorage = new FileStorage();
                IFileStorageImpl storageImpl = fileStorage.Build();
                IElasticConnection elasticConnection = new ElasticConnectionBuilder().Build(configuration);
                ILocationFileReaderImpl locationReader = new LocationFileReader().Build();
                IOwmApiReader owmReader = new OwmApiReader();

                IOwmToElasticDocumentConverter<CurrentWeatherBase> owmConverter = new OwmToElasticDocumentConverter();

                var processor =
                    new ProcessingBaseCurrentWeatherImpl(elasticConnection, locationReader, owmReader, storageImpl,
                        owmConverter);


                await processor.Process(configuration);

            });
        }
    }
}