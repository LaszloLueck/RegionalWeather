using System.Diagnostics;
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Processing;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;
using Serilog;

namespace RegionalWeather.Scheduler
{
    public class ReindexerSchedulerJobWeather : IJob
    {

        private readonly ILogger _logger;
        public ReindexerSchedulerJobWeather()
        {
            _logger = Log.Logger.ForContext<ReindexerSchedulerJobWeather>();
        }
        
        public async Task Execute(IJobExecutionContext context)
        {
            var sw = Stopwatch.StartNew();
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];

            try
            {
                await Task.Run(async () =>
                {
                    _logger.Information("Check if any reindex job todo.");
                    IElasticConnection elasticConnection =
                        new ElasticConnectionBuilder().Build(configuration);
                    IOwmToElasticDocumentConverter<CurrentWeatherBase> owmConverter =
                        new OwmToElasticDocumentConverter();
                    IDirectoryUtils directoryUtils = new DirectoryUtils();
                    IProcessingBaseImplementations processingBaseImplementations =
                        new ProcessingBaseImplementations();

                    var processor = new ProcessingBaseReIndexerGenericImpl<CurrentWeatherBase>(elasticConnection,
                        owmConverter, directoryUtils, processingBaseImplementations);

                    await processor.Process<WeatherLocationDocument>(configuration, configuration.ElasticIndexName,
                        "FileStorage_*.dat", "FileStorage_", ".dat");

                });
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "ReindexerSchedulerJobWeather.Execute",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}