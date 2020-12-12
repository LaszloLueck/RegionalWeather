using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Quartz;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Logging;
using RegionalWeather.Owm;
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
                await Log.InfoAsync("Check if any reindex job todo.");
                if (!Directory.Exists(configuration.ReindexLookupPath))
                {
                    await Log.WarningAsync("Reindex lookup directory does not exist. Finished funcion!");
                }
                else
                {
                    var files = Directory.GetFiles(configuration.ReindexLookupPath, "FileStorage_*.dat");
                    IElasticConnectionBuilder connectionBuilder =
                        new ElasticConnectionBuilder();
                    var elasticConnection = connectionBuilder.Build(configuration);

                    if (!elasticConnection.IndexExists(configuration.ElasticIndexName))
                    {
                        if (elasticConnection.CreateIndex(configuration.ElasticIndexName))
                        {
                            await Log.InfoAsync($"index {configuration.ElasticIndexName} successfully created");
                        }
                        else
                        {
                            await Log.WarningAsync($"error while create index {configuration.ElasticIndexName}");
                        }
                    }


                    (from file in files select file)
                        .ToList()
                        .ForEach(file =>
                        {
                            Log.Info($"Backup Data from file {file}");
                            File.ReadAllLines(file)
                                .Select(loc => JsonSerializer.Deserialize<Root>(loc))
                                .Where(element => element != null)
                                .Select(element => element!)
                                .Select(OwmToElasticDocumentConverter.Convert)
                                .Select((owm, index) => new {owm, index})
                                .GroupBy(g => g.index / 100, o => o.owm)
                                .ToList()
                                .ForEach(k =>
                                    elasticConnection.BulkWriteDocument(k, configuration.ElasticIndexName));

                            Log.Info($"Remove the file <{file}> after indexing");
                            File.Delete(file);
                        });

                }
            });
        }
    }
}