#nullable enable
using System.Diagnostics;
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
            var sw = Stopwatch.StartNew();
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];

            var logger = Log.Logger.ForContext<CurrentWeatherSchedulerJob>();
            try
            {
                await Task.Run(async () =>
                {
                    logger.Information("Use the following parameters for this job:");
                    logger.Information($"Parallelism: {configuration.Parallelism}");
                    logger.Information($"Runs every: {configuration.RunsEvery} s");
                    logger.Information($"Path to Locations file: {configuration.PathToLocationsMap}");
                    logger.Information($"Write to Elastic index: {configuration.ElasticIndexName}");
                    logger.Information($"ElasticSearch: {configuration.ElasticHostsAndPorts}");

                    IFileStorage storage = new FileStorageImpl();
                    IProcessingUtils processingUtils = new ProcessingUtils(storage);
                    IElasticConnection elasticConnection =
                        new ElasticConnectionBuilder().Build(configuration);
                    ILocationFileReader locationReader = new LocationFileReaderImpl();
                    IOwmApiReader owmReader = new OwmApiReader();
                    IOwmToElasticDocumentConverter<CurrentWeatherBase> owmConverter =
                        new OwmToElasticDocumentConverter();
                    IProcessingBaseImplementations processingBaseImplementations =
                        new ProcessingBaseImplementations();

                    var processor =
                        new ProcessingBaseCurrentWeatherImpl(elasticConnection, locationReader, owmReader,
                            processingUtils,
                            owmConverter, processingBaseImplementations);


                    await processor.Process(configuration);

                });
            }
            finally
            {
                sw.Stop();
                logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "CurrentWeatherSchedulerJob.Execute",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}