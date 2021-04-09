using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Elastic;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseReIndexerProxy<T> : IProcessingBaseImplementations, IElasticConnection,
        IOwmToElasticDocumentConverter<T>
    {
        private readonly IElasticConnection _elasticConnection;
        private readonly IProcessingBaseImplementations _processingBaseImplementations;
        private readonly IOwmToElasticDocumentConverter<T> _owmToElasticDocumentConverter;

        protected ProcessingBaseReIndexerProxy(IElasticConnection elasticConnection,
            IProcessingBaseImplementations processingBaseImplementations,
            IOwmToElasticDocumentConverter<T> owmToElasticDocumentConverter)
        {
            _elasticConnection = elasticConnection;
            _processingBaseImplementations = processingBaseImplementations;
            _owmToElasticDocumentConverter = owmToElasticDocumentConverter;
        }

        public Task<Option<X>> DeserializeObjectAsync<X>(string data) =>
            _processingBaseImplementations.DeserializeObjectAsync<X>(data);

        public ParallelQuery<X> ConvertToParallelQuery<X>(IEnumerable<X> queryable, int parallelism) =>
            _processingBaseImplementations.ConvertToParallelQuery(queryable, parallelism);

        public string BuildIndexName(string indexName, DateTime shardDatetime) =>
            _elasticConnection.BuildIndexName(indexName, shardDatetime);

        public async Task BulkWriteDocumentsAsync<X>(IEnumerable<X> documents, string indexName)
            where X : ElasticDocument =>
            await _elasticConnection.BulkWriteDocumentsAsync(documents, indexName);

        public async Task<bool> IndexExistsAsync(string indexName) =>
            await _elasticConnection.IndexExistsAsync(indexName);

        public async Task<bool> CreateIndexAsync<X>(string indexName) where X : ElasticDocument =>
            await _elasticConnection.CreateIndexAsync<X>(indexName);

        public async Task<bool> DeleteIndexAsync(string indexName) =>
            await _elasticConnection.DeleteIndexAsync(indexName);

        public async Task<bool> RefreshIndexAsync(string indexName) =>
            await _elasticConnection.RefreshIndexAsync(indexName);

        public async Task<bool> FlushIndexAsync(string indexName) =>
            await _elasticConnection.FlushIndexAsync(indexName);

        public Task<Option<ElasticDocument>> ConvertAsync(T owmDoc) =>
            _owmToElasticDocumentConverter.ConvertAsync(owmDoc);
    }
}