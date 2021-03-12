using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Logging;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Processing;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Scheduler
{
    public class ReindexerSchedulerJobWeather : IJob
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<ReindexerSchedulerJobWeather>.GetLogger();

        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            await Task.Run(async () =>
            {
                await Log.InfoAsync("Check if any reindex job todo.");
                IElasticConnection elasticConnection = new ElasticConnectionBuilder().Build(configuration);
                IOwmToElasticDocumentConverter<CurrentWeatherBase> owmConverter = new OwmToElasticDocumentConverter();
                IDirectoryUtils directoryUtils = new DirectoryUtils();

                var processor =
                    new ProcessingBaseReIndexerWeatherImpl(elasticConnection, owmConverter, directoryUtils);

                await processor.Process(configuration);
            });
        }
    }
}