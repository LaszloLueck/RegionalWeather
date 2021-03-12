using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Optional.Collections;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Logging;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseReIndexerAirPollutionImpl : ProcessingBaseReIndexerAirPollution
    {
        private static readonly IMySimpleLogger
            Log = MySimpleLoggerImpl<ProcessingBaseReIndexerAirPollutionImpl>.GetLogger();

        public ProcessingBaseReIndexerAirPollutionImpl(IElasticConnection elasticConnection,
            IOwmToElasticDocumentConverter<AirPollutionBase> owmDocumentConverter,
            IDirectoryUtils directoryUtils) : base(
            elasticConnection, owmDocumentConverter, directoryUtils)
        {
        }

        public override async Task Process(ConfigurationItems configuration)
        {
            await Task.Run(async () =>
            {
                var continueWithDirectory = true;
                if (!DirectoryExists(configuration.ReindexLookupPath))
                {
                    await Log.WarningAsync("Reindex lookup directory does not exist. Lets create it");
                    continueWithDirectory = CreateDirectory(configuration.ReindexLookupPath);
                }

                if (continueWithDirectory)
                {
                    var files = GetFilesOfDirectory(configuration.ReindexLookupPath,
                        "AirPollution_*.dat");

                    //Create the indexes from files
                    var distinct = files
                        .Select(file =>
                            BuildIndexName(configuration.AirPollutionIndexName, GenerateIndexDateFromFileName(file)))
                        .Distinct();

                    var createIndexTasks = distinct
                        .Select(async indexName =>
                        {
                            if (!await IndexExistsAsync(indexName))
                            {
                                await CreateIndexAsync<AirPollutionDocument>(indexName);
                                await RefreshIndexAsync(indexName);
                            }
                        });

                    await Task.WhenAll(createIndexTasks);

                    var tasks = (from file in files select file)
                        .Select(async file =>
                        {
                            await Log.InfoAsync($"Restore data from file <{file}>");
                            var elements = ReadAllLinesOfFile(file);

                            var convertedElementTasks = elements
                                .Select(async element => await DeserializeObjectAsync<AirPollutionBase>(element));

                            var convertedElements = (await Task.WhenAll(convertedElementTasks))
                                .Values();

                            var convertedIndexDocsTasks = convertedElements
                                .Select(async element => await ConvertAsync(element));

                            var convertedIndexDocs = (await Task.WhenAll(convertedIndexDocsTasks)).Values();

                            var indexName = BuildIndexName(configuration.AirPollutionIndexName,
                                GenerateIndexDateFromFileName(file));

                            convertedIndexDocs
                                .Select((owm, index) => new {owm, index})
                                .GroupBy(g => g.index / 100, o => o.owm)
                                .ToList()
                                .ForEach(async group =>
                                    await BulkWriteDocumentsAsync(group, indexName));

                            await FlushIndexAsync(indexName);
                            await Log.InfoAsync($"Remove the file <{file}> after indexing");
                            await Task.Run(() => DeleteFile(file));
                        });

                    await Task.WhenAll(tasks);
                }
            });
        }

        private static DateTime GenerateIndexDateFromFileName(string fileName)
        {
            var subSeq = fileName.IndexOf("AirPollution_");
            var fn = fileName.Substring(subSeq);


            var newName = fn.Replace("AirPollution_", "").Replace(".dat", "");

            return DateTime.ParseExact(newName, "yyyyMMdd", CultureInfo.InvariantCulture);
        }
    }
}