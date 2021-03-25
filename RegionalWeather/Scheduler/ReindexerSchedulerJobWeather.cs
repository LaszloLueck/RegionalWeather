using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Logging;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Processing;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;
using Serilog;

namespace RegionalWeather.Scheduler
{
    public abstract class ReindexerSchedulerJobWeather : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            var loggingBase = (ILogger) context.JobDetail.JobDataMap["loggingBase"];
            var logger = loggingBase.ForContext<ReindexerSchedulerJobAirPollution>();

            await Task.Run(async () =>
            {
                logger.Information("Check if any reindex job todo.");
                IElasticConnection elasticConnection = new ElasticConnectionBuilder().Build(configuration, loggingBase);
                IOwmToElasticDocumentConverter<CurrentWeatherBase> owmConverter =
                    new OwmToElasticDocumentConverter(loggingBase);
                IDirectoryUtils directoryUtils = new DirectoryUtils(loggingBase);
                IProcessingBaseImplementations processingBaseImplementations =
                    new ProcessingBaseImplementations(loggingBase);

                var processor =
                    new ProcessingBaseReIndexerWeatherImpl(elasticConnection, owmConverter, directoryUtils, loggingBase,
                        processingBaseImplementations);

                await processor.Process(configuration);
            });
        }
    }
}