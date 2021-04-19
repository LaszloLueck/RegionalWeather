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
    public class ProcessingBaseReIndexerGenericImpl<T>
    {
        private readonly ILogger _logger;
        private readonly IElasticConnection _elasticConnection;
        private readonly IOwmToElasticDocumentConverter<T> _owmToElasticDocumentConverter;
        private readonly IDirectoryUtils _directoryUtils;
        private readonly IProcessingBaseImplementations _processingBaseImplementations;
        
        public ProcessingBaseReIndexerGenericImpl(IElasticConnection elasticConnection,
            IOwmToElasticDocumentConverter<T> owmDocumentConverter, IDirectoryUtils directoryUtils, ILogger loggingBase,
            IProcessingBaseImplementations processingBaseImplementations)
        {
            _logger = loggingBase.ForContext<ProcessingBaseReIndexerGenericImpl<T>>();
            _elasticConnection = elasticConnection;
            _owmToElasticDocumentConverter = owmDocumentConverter;
            _directoryUtils = directoryUtils;
            _processingBaseImplementations = processingBaseImplementations;
        }

        public async Task Process<X>(ConfigurationItems configuration, string indexName, string filePattern, string filPrefix, string fileSuffix) where X : ElasticDocument
        {
            await Task.Run(async () =>
            {
                var continueWithDirectory = true;
                if (!_directoryUtils.DirectoryExists(configuration.ReindexLookupPath))
                {
                    _logger.Warning("Reindex lookup directory does not exist. Lets create it");
                    continueWithDirectory = _directoryUtils.CreateDirectory(configuration.ReindexLookupPath);
                }

                if (continueWithDirectory)
                {
                    var files = _directoryUtils.GetFilesOfDirectory(configuration.ReindexLookupPath,
                        filePattern);

                    _logger.Information($"found files for pattern {filePattern}");
                    
                    //Create the indexes from files
                    var distinct = files
                        .Select(file =>
                            _elasticConnection.BuildIndexName(indexName, GenerateIndexDateFromFileName(file, filPrefix, fileSuffix)))
                        .Distinct();

                    var createIndexTasks = distinct
                        .Select(async indexName =>
                        {
                            if (!await _elasticConnection.IndexExistsAsync(indexName))
                            {
                                await _elasticConnection.CreateIndexAsync<X>(indexName);
                                await _elasticConnection.RefreshIndexAsync(indexName);
                            }
                        });

                    await Task.WhenAll(createIndexTasks);

                    var tasks = (from file in files select file)
                        .Select(async file =>
                        {
                            _logger.Information($"Restore data from file <{file}>");
                            var elements = _directoryUtils.ReadAllLinesOfFile(file);

                            var convertedElementTasks = elements
                                .Select(async element => await _processingBaseImplementations.DeserializeObjectAsync<T>(element));

                            var convertedElements = (await Task.WhenAll(convertedElementTasks))
                                .Values();

                            var convertedIndexDocsTasks = convertedElements
                                .Select(async element => await _owmToElasticDocumentConverter.ConvertAsync(element));

                            var convertedIndexDocs = (await Task.WhenAll(convertedIndexDocsTasks)).Values();

                            var indexName = _elasticConnection.BuildIndexName(configuration.AirPollutionIndexName,
                                GenerateIndexDateFromFileName(file, filPrefix, fileSuffix));

                            convertedIndexDocs
                                .Select((owm, index) => new {owm, index})
                                .GroupBy(g => g.index / 100, o => o.owm)
                                .ToList()
                                .ForEach(async group =>
                                    await _elasticConnection.BulkWriteDocumentsAsync(group, indexName));

                            await _elasticConnection.FlushIndexAsync(indexName);
                            _logger.Information($"Remove the file <{file}> after indexing");
                            await Task.Run(() => _directoryUtils.DeleteFile(file));
                        });

                    await Task.WhenAll(tasks);
                }
            });
        }

        private static DateTime GenerateIndexDateFromFileName(string fileName, string prefix, string suffix)
        {
            var subSeq = fileName.IndexOf(prefix);
            var fn = fileName.Substring(subSeq);
            var newName = fn.Replace(prefix, "").Replace(suffix, "");
            return DateTime.ParseExact(newName, "yyyyMMdd", CultureInfo.InvariantCulture);
        }

    }
}