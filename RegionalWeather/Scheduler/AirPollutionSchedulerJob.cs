using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Processing;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;
using Serilog;

namespace RegionalWeather.Scheduler
{
    public class AirPollutionSchedulerJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            var loggingBase = (ILogger) context.JobDetail.JobDataMap["loggingBase"];
            var logger = loggingBase.ForContext<AirPollutionSchedulerJob>();

            await Task.Run(async () =>
            {
                logger.Information("Use the following parameters for this job");
                logger.Information($"Parallelism: {configuration.Parallelism}");
                logger.Information($"Runs every {configuration.AirPollutionRunsEvery} s");
                logger.Information($"Path to Locations file: {configuration.AirPollutionLocationsFile}");
                logger.Information($"Write to Elastic index {configuration.AirPollutionIndexName}");
                logger.Information($"ElasticSearch: {configuration.ElasticHostsAndPorts}");

                IElasticConnection elasticConnection = new ElasticConnectionBuilder().Build(configuration, loggingBase);
                ILocationFileReader locationReader = new LocationFileReaderImpl(loggingBase);
                IFileStorage fileStorage = new FileStorageImpl(loggingBase);
                IProcessingUtils processingUtils = new ProcessingUtils(fileStorage, loggingBase);
                IOwmApiReader owmApiReader = new OwmApiReader(loggingBase);
                IOwmToElasticDocumentConverter<AirPollutionBase> owmConverter =
                    new AirPollutionToElasticDocumentConverter(loggingBase);
                IProcessingBaseImplementations processingBaseImplementations =
                    new ProcessingBaseImplementations(loggingBase);

                var processor = new ProcessingBaseAirPollutionImpl(elasticConnection, locationReader, processingUtils,
                    owmApiReader, owmConverter, processingBaseImplementations, loggingBase);


                await processor.Process(configuration);
            });
        }
    }
}