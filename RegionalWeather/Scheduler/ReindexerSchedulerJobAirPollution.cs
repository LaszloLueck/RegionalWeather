using System.Diagnostics;
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Processing;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;
using Serilog;

namespace RegionalWeather.Scheduler
{
    public class ReindexerSchedulerJobAirPollution : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var sw = Stopwatch.StartNew();
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            var loggingBase = (ILogger) context.JobDetail.JobDataMap["loggingBase"];
            var logger = loggingBase.ForContext<ReindexerSchedulerJobAirPollution>();

            try
            {
                await Task.Run(async () =>
                {
                    logger.Information("Check if any reindex job todo.");
                    IElasticConnection elasticConnection =
                        new ElasticConnectionBuilder().Build(configuration);
                    IOwmToElasticDocumentConverter<AirPollutionBase> owmConverter =
                        new AirPollutionToElasticDocumentConverter();
                    IDirectoryUtils directoryUtils = new DirectoryUtils();
                    IProcessingBaseImplementations processingBaseImplementations =
                        new ProcessingBaseImplementations();

                    var processor = new ProcessingBaseReIndexerGenericImpl<AirPollutionBase>(elasticConnection,
                        owmConverter, directoryUtils, processingBaseImplementations);

                    await processor.Process<AirPollutionDocument>(configuration, configuration.AirPollutionIndexName,
                        "AirPollution_*.dat", "AirPollution_", ".dat");

                });
            }
            finally
            {
                sw.Stop();
                logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "ReindexerSchedulerJobAirPollution.Execute",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}