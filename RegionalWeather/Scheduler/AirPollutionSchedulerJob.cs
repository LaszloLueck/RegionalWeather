using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Logging;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Processing;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Scheduler
{
    public class AirPollutionSchedulerJob : IJob
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<AirPollutionSchedulerJob>.GetLogger();

        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];

            await Task.Run(async () =>
            {
                await Log.InfoAsync("Use the following parameters for this job");
                await Log.InfoAsync($"Parallelism: {configuration.Parallelism}");
                await Log.InfoAsync($"Runs every {configuration.AirPollutionRunsEvery} s");
                await Log.InfoAsync($"Path to Locations file: {configuration.AirPollutionLocationsFile}");
                await Log.InfoAsync($"Write to Elastic index {configuration.AirPollutionIndexName}");
                await Log.InfoAsync($"ElasticSearch: {configuration.ElasticHostsAndPorts}");

                IElasticConnection elasticConnection = new ElasticConnectionBuilder().Build(configuration);
                ILocationFileReaderImpl locationReader = new LocationFileReader().Build();
                IFileStorageImpl fileStorageImpl = new FileStorage().Build();
                IOwmApiReader owmApiReader = new OwmApiReader();
                IOwmToElasticDocumentConverter<AirPollutionBase> owmConverter =
                    new AirPollutionToElasticDocumentConverter();

                var processor = new ProcessingBaseAirPollutionImpl(elasticConnection, locationReader, fileStorageImpl,
                    owmApiReader, owmConverter);


                await processor.Process(configuration);
                
            });
        }
    }
}