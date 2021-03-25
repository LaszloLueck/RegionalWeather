using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Optional.Collections;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;
using Serilog;

namespace RegionalWeather.Processing
{
    public class ProcessingBaseReIndexerAirPollutionImpl : ProcessingBaseReIndexerAirPollution
    {
        private readonly ILogger _logger;

        public ProcessingBaseReIndexerAirPollutionImpl(IElasticConnection elasticConnection,
            IOwmToElasticDocumentConverter<AirPollutionBase> owmDocumentConverter,
            IDirectoryUtils directoryUtils, ILogger loggingBase, IProcessingBaseImplementations processingBaseImplementations) : base(
            elasticConnection, owmDocumentConverter, directoryUtils, processingBaseImplementations)
        {
            _logger = loggingBase.ForContext<ProcessingBaseReIndexerAirPollutionImpl>();
        }

        public override async Task Process(ConfigurationItems configuration)
        {
            await Task.Run(async () =>
            {
                var continueWithDirectory = true;
                if (!DirectoryExists(configuration.ReindexLookupPath))
                {
                    _logger.Warning("Reindex lookup directory does not exist. Lets create it");
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
                            _logger.Information($"Restore data from file <{file}>");
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
                            _logger.Information($"Remove the file <{file}> after indexing");
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