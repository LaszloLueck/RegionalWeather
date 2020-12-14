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
                    await Log.WarningAsync("Reindex lookup directory does not exist. Lets create it");
                    Directory.CreateDirectory(configuration.ReindexLookupPath);
                }
                else
                {
                    var files = Directory.GetFiles(configuration.ReindexLookupPath, "FileStorage_*.dat");
                    IElasticConnectionBuilder connectionBuilder =
                        new ElasticConnectionBuilder();
                    var elasticConnection = connectionBuilder.Build(configuration);

                    if (!await elasticConnection.IndexExistsAsync(configuration.ElasticIndexName))
                    {
                        if (await elasticConnection.CreateIndexAsync(configuration.ElasticIndexName))
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
                        .ForEach(async file =>
                        {
                            await Log.InfoAsync($"Backup Data from file {file}");
                            var elements = (await File.ReadAllLinesAsync(file))
                                .Select(loc => JsonSerializer.Deserialize<Root>(loc))
                                .Where(element => element != null)
                                .Select(element => element!);


                            var fp = elements.Select(async element =>
                                await OwmToElasticDocumentConverter.ConvertAsync(element));

                            (await Task.WhenAll(fp))
                                .Select((owm, index) => new {owm, index})
                                .GroupBy(g => g.index / 100, o => o.owm)
                                .ToList()
                                .ForEach(async k =>
                                    await elasticConnection.BulkWriteDocumentsAsync(k, configuration.ElasticIndexName));
                            await Log.InfoAsync($"Remove the file <{file}> after indexing");
                            File.Delete(file);
                        });
                }
            });
        }
    }
}