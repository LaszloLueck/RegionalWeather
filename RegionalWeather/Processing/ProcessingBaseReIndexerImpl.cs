using System.Linq;
using System.Threading.Tasks;
using Optional.Collections;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Logging;
using RegionalWeather.Owm;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseReIndexerImpl : ProcessingBaseReIndexer
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<ProcessingBaseReIndexerImpl>.GetLogger();

        public ProcessingBaseReIndexerImpl(IElasticConnection elasticConnection,
            IOwmToElasticDocumentConverter owmDocumentConverter, IDirectoryUtils directoryUtils) : base(
            elasticConnection, owmDocumentConverter, directoryUtils)
        {
        }

        public override async Task Process(ConfigurationItems configuration)
        {
            await Task.Run(async () =>
            {
                var continueWithDirectory = true;
                if (DirectoryExists(configuration.ReindexLookupPath))
                {
                    await Log.WarningAsync("Reindex lookup directory does not exist. Lets create it");
                    continueWithDirectory = CreateDirectory(configuration.ReindexLookupPath);
                }

                if (continueWithDirectory)
                {
                    var elasticIndexSuccess = true;
                    if (!await IndexExistsAsync(configuration.ElasticIndexName))
                    {
                        elasticIndexSuccess = await CreateIndexAsync(configuration.ElasticIndexName);
                    }

                    if (elasticIndexSuccess)
                    {
                        var files = GetFilesOfDirectory(configuration.ReindexLookupPath,
                            "FileStorage_*.dat");
                        var tasks = (from file in files select file)
                            .Select(async file =>
                            {
                                await Log.InfoAsync($"Restore data from file <{file}>");
                                //var elements = await ReadAllLinesOfFileAsync(file);
                                var elements = ReadAllLinesOfFile(file);
                                
                                var convertedElementTasks = elements
                                    .Select(async element => await DeserializeObjectAsync<Root>(element));

                                var convertedElements = (await Task.WhenAll(convertedElementTasks))
                                    .Values();

                                var convertedIndexDocs = convertedElements
                                    .Select(async element => await ConvertAsync(element));

                                (await Task.WhenAll(convertedIndexDocs))
                                    .Values()
                                    .Select((owm, index) => new {owm, index})
                                    .GroupBy(g => g.index / 100, o => o.owm)
                                    .ToList()
                                    .ForEach(async group =>
                                        await BulkWriteDocumentsAsync(group,
                                            configuration.ElasticIndexName));

                                await Log.InfoAsync($"Remove the file <{file}> after indexing");
                                await Task.Run(() => DeleteFile(file));
                            });

                        await Task.WhenAll(tasks.ToList());
                    }
                }
            });
        }
    }
}