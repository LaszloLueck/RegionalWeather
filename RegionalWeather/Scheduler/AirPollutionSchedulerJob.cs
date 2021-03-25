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
using Serilog;

namespace RegionalWeather.Scheduler
{
    public abstract class AirPollutionSchedulerJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            var loggingBase = (ILogger) context.JobDetail.JobDataMap["loggingBase"];
            var logger = loggingBase.ForContext<ReindexerSchedulerJobAirPollution>();

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
                IOwmApiReader owmApiReader = new OwmApiReader(loggingBase);
                IOwmToElasticDocumentConverter<AirPollutionBase> owmConverter =
                    new AirPollutionToElasticDocumentConverter(loggingBase);
                IProcessingBaseImplementations processingBaseImplementations =
                    new ProcessingBaseImplementations(loggingBase);
                
                var processor = new ProcessingBaseAirPollutionImpl(elasticConnection, locationReader, fileStorage,
                    owmApiReader, owmConverter, processingBaseImplementations);


                await processor.Process(configuration);
                
            });
        }
    }
}