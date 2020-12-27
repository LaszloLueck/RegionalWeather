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
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<ProcessingBaseCurrentWeatherImpl>.GetLogger();

        public ProcessingBaseReIndexerImpl(ElasticConnection elasticConnection,
            OwmToElasticDocumentConverter owmToElasticDocumentConverter, IDirectoryUtils directoryUtilsImpl) : base(
            elasticConnection, owmToElasticDocumentConverter, directoryUtilsImpl)
        {
        }

        public override async Task Process(ConfigurationItems configuration)
        {
            await Task.Run(async () =>
            {
                var files = DirectoryUtilsImpl.GetFilesOfDirectory(configuration.ReindexLookupPath,
                    "FileStorage_*.dat");
                var tasks = (from file in files select file)
                    .Select(async file =>
                    {
                        await Log.InfoAsync($"Restore data from file <{file}>");
                        var elements = await DirectoryUtilsImpl.ReadAllLinesOfFileAsync(file);
                        
                        var convertedElementTasks = elements
                            .Select(async element => await DeserializeObjectAsync<Root>(element));

                        var convertedElements = (await Task.WhenAll(convertedElementTasks))
                            .Values();

                        var convertedIndexDocs = convertedElements
                            .Select(async element => await OwmToElasticDocumentConverterImpl.ConvertAsync(element));

                        (await Task.WhenAll(convertedIndexDocs))
                            .Values()
                            .Select((owm, index) => new {owm, index})
                            .GroupBy(g => g.index / 100, o => o.owm)
                            .ToList()
                            .ForEach(async group =>
                                await ElasticConnectionImpl.BulkWriteDocumentsAsync(group,
                                    configuration.ElasticIndexName));

                        await Log.InfoAsync($"Remove the file <{file}> after indexing");
                        await Task.Run(() => DeleteFile(file));
                    });

                await Task.WhenAll(tasks.ToList());

            });
        }
    }
}