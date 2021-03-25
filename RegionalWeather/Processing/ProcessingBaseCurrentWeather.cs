#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseCurrentWeather : IProcessingBaseImplementations, IElasticConnection,
        ILocationFileReader, IOwmApiReader, IFileStorage, IOwmToElasticDocumentConverter<CurrentWeatherBase>,
        IProcessingBase
    {
        private readonly IElasticConnection _elasticConnection;
        private readonly ILocationFileReader _locationFileReader;
        private readonly IOwmApiReader _owmApiReader;
        private readonly IFileStorage _fileStorage;
        private readonly IOwmToElasticDocumentConverter<CurrentWeatherBase> _owmConverter;
        private readonly IProcessingBaseImplementations _processingBaseImplementations;

        protected ProcessingBaseCurrentWeather(IElasticConnection elasticConnection,
            ILocationFileReader locationFileReader,
            IOwmApiReader owmApiReader, IFileStorage fileStorage,
            IOwmToElasticDocumentConverter<CurrentWeatherBase> owmToElasticDocumentConverter, IProcessingBaseImplementations processingBaseImplementations)
        {
            _elasticConnection = elasticConnection;
            _locationFileReader = locationFileReader;
            _owmApiReader = owmApiReader;
            _fileStorage = fileStorage;
            _owmConverter = owmToElasticDocumentConverter;
            _processingBaseImplementations = processingBaseImplementations;
        }

        public Task<Option<T>> DeserializeObjectAsync<T>(string data) =>
            _processingBaseImplementations.DeserializeObjectAsync<T>(data);

        public ParallelQuery<T> ConvertToParallelQuery<T>(IEnumerable<T> queryable, int parallelism) =>
            _processingBaseImplementations.ConvertToParallelQuery(queryable, parallelism);

        public Task<bool> RefreshIndexAsync(string indexName) => _elasticConnection.RefreshIndexAsync(indexName);

        public Task<bool> FlushIndexAsync(string indexName) => _elasticConnection.FlushIndexAsync(indexName);

        public string BuildIndexName(string indexName, DateTime shardDatetime) =>
            _elasticConnection.BuildIndexName(indexName, shardDatetime);

        public async Task<Option<ElasticDocument>> ConvertAsync(CurrentWeatherBase owmDoc) =>
            await _owmConverter.ConvertAsync(owmDoc);

        public async Task<Option<List<string>>> ReadLocationsAsync(string locationPath) =>
            await _locationFileReader.ReadLocationsAsync(locationPath);

        public async Task WriteAllDataAsync<T>(IEnumerable<T> roots, string fileName) =>
            await _fileStorage.WriteAllDataAsync(roots, fileName);

        public async Task<Option<string>> ReadDataFromLocationAsync(string url) =>
            await _owmApiReader.ReadDataFromLocationAsync(url);

        public async Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : ElasticDocument => await _elasticConnection.BulkWriteDocumentsAsync(documents, indexName);

        public async Task<bool> DeleteIndexAsync(string indexName) =>
            await _elasticConnection.DeleteIndexAsync(indexName);

        public async Task<bool> IndexExistsAsync(string indexName) =>
            await _elasticConnection.IndexExistsAsync(indexName);

        public async Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument =>
            await _elasticConnection.CreateIndexAsync<WeatherLocationDocument>(indexName);

        public abstract Task Process(ConfigurationItems configuration);
    }
}