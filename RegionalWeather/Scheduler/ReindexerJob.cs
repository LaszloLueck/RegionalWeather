using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Logging;
using RegionalWeather.Owm;
using RegionalWeather.Processing;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Scheduler
{
    public class ReindexerJob : IJob
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<ReindexerJob>.GetLogger();

        public async Task Execute(IJobExecutionContext context)
        {
            var configuration = (ConfigurationItems) context.JobDetail.JobDataMap["configuration"];
            await Task.Run(async () =>
            {
                IElasticConnectionBuilder connectionBuilder =
                    new ElasticConnectionBuilder();
                var elasticConnection = connectionBuilder.Build(configuration);
                var owmConverter = new OwmToElasticDocumentConverter();
                IDirectoryUtils directoryUtils = new DirectoryUtils();

                var processor =
                    new ProcessingBaseReIndexerImpl(elasticConnection, owmConverter, directoryUtils);

                await Log.InfoAsync("Check if any reindex job todo.");
                var continueWithDirectory = true;
                
                if (!processor.DirectoryExists(configuration.ReindexLookupPath))
                {
                    await Log.WarningAsync("Reindex lookup directory does not exist. Lets create it");
                    continueWithDirectory = processor.CreateDirectory(configuration.ReindexLookupPath);
                }

                if (continueWithDirectory)
                {
                    var elasticIndexSuccess = true;
                    if (!await processor.ElasticIndexExistsAsync(configuration.ElasticIndexName))
                    {
                        elasticIndexSuccess = await processor.CreateIndexAsync(configuration.ElasticIndexName);
                    }

                    if (elasticIndexSuccess)
                    {
                        await processor.Process(configuration);
                    }
                }
            });
        }
    }
}