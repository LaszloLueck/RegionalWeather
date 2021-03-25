#nullable enable
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Processing;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;
using Serilog;

namespace RegionalWeather.Scheduler
{
    public class CurrentWeatherSchedulerJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            var loggingBase = (ILogger) context.JobDetail.JobDataMap["loggingBase"];
            var logger = loggingBase.ForContext<CurrentWeatherSchedulerJob>();
            
            await Task.Run(async () =>
            {
                logger.Information("Use the following parameters for this job:");
                logger.Information($"Parallelism: {configuration.Parallelism}");
                logger.Information($"Runs every: {configuration.RunsEvery} s");
                logger.Information($"Path to Locations file: {configuration.PathToLocationsMap}");
                logger.Information($"Write to Elastic index: {configuration.ElasticIndexName}");
                logger.Information($"ElasticSearch: {configuration.ElasticHostsAndPorts}");
                
                IFileStorage storage = new FileStorageImpl(loggingBase);
                IElasticConnection elasticConnection = new ElasticConnectionBuilder().Build(configuration, loggingBase);
                ILocationFileReader locationReader = new LocationFileReaderImpl(loggingBase);
                IOwmApiReader owmReader = new OwmApiReader(loggingBase);
                IOwmToElasticDocumentConverter<CurrentWeatherBase> owmConverter = new OwmToElasticDocumentConverter(loggingBase);
                IProcessingBaseImplementations processingBaseImplementations =
                    new ProcessingBaseImplementations(loggingBase);
                
                var processor =
                    new ProcessingBaseCurrentWeatherImpl(elasticConnection, locationReader, owmReader, storage,
                        owmConverter, processingBaseImplementations);


                await processor.Process(configuration);

            });
        }
    }
}