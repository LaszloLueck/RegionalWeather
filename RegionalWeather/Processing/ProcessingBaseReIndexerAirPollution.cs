using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseReIndexerAirPollution : IProcessingBaseImplementations, IDirectoryUtils,
        IElasticConnection,
        IOwmToElasticDocumentConverter<AirPollutionBase>, IProcessingBase
    {
        private readonly IElasticConnection _elasticConnection;
        private readonly IOwmToElasticDocumentConverter<AirPollutionBase> _owmDocumentConverter;
        private readonly IDirectoryUtils _directoryUtils;
        private readonly IProcessingBaseImplementations _processingBaseImplementations;


        protected ProcessingBaseReIndexerAirPollution(IElasticConnection elasticConnection,
            IOwmToElasticDocumentConverter<AirPollutionBase> owmDocumentConverter, IDirectoryUtils directoryUtils, IProcessingBaseImplementations processingBaseImplementations)
        {
            _elasticConnection = elasticConnection;
            _owmDocumentConverter = owmDocumentConverter;
            _directoryUtils = directoryUtils;
            _processingBaseImplementations = processingBaseImplementations;
        }

        public Task<Option<T>> DeserializeObjectAsync<T>(string data) =>
            _processingBaseImplementations.DeserializeObjectAsync<T>(data);

        public ParallelQuery<T> ConvertToParallelQuery<T>(IEnumerable<T> queryable, int parallelism) =>
            _processingBaseImplementations.ConvertToParallelQuery(queryable, parallelism);

        public Task<Option<ElasticDocument>> ConvertAsync(AirPollutionBase owmDoc) =>
            _owmDocumentConverter.ConvertAsync(owmDoc);

        public Task<bool> RefreshIndexAsync(string indexName) => _elasticConnection.RefreshIndexAsync(indexName);

        public Task<bool> FlushIndexAsync(string indexName) => _elasticConnection.FlushIndexAsync(indexName);

        public string BuildIndexName(string indexName, DateTime shardDatetime) =>
            _elasticConnection.BuildIndexName(indexName, shardDatetime);

        public IEnumerable<string> ReadAllLinesOfFile(string path) => _directoryUtils.ReadAllLinesOfFile(path);

        public async Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : ElasticDocument =>
            await _elasticConnection.BulkWriteDocumentsAsync(documents, indexName);

        public async Task<bool> DeleteIndexAsync(string indexName) =>
            await _elasticConnection.DeleteIndexAsync(indexName);

        public async Task<bool> IndexExistsAsync(string indexName) =>
            await _elasticConnection.IndexExistsAsync(indexName);

        public async Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument =>
            await _elasticConnection.CreateIndexAsync<WeatherLocationDocument>(indexName);

        public bool DirectoryExists(string path) => _directoryUtils.DirectoryExists(path);

        public bool CreateDirectory(string path) => _directoryUtils.CreateDirectory(path);

        public IEnumerable<string> GetFilesOfDirectory(string path, string filePattern) =>
            _directoryUtils.GetFilesOfDirectory(path, filePattern);

        public bool DeleteFile(string path) => _directoryUtils.DeleteFile(path);

        public abstract Task Process(ConfigurationItems configuration);
    }
}