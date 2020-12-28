#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Owm;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseCurrentWeather : ProcessingBaseImplementations, IElasticConnection, ILocationFileReaderImpl, IOwmApiReader, IFileStorageImpl, IOwmToElasticDocumentConverter, IProcessingBase
    {
        private readonly IElasticConnection _elasticConnection;
        private readonly ILocationFileReaderImpl _locationFileReader;
        private readonly IOwmApiReader _owmApiReader;
        private readonly IFileStorageImpl _fileStorage;
        private readonly IOwmToElasticDocumentConverter _owmConverter;


        protected ProcessingBaseCurrentWeather(IElasticConnection elasticConnection, ILocationFileReaderImpl locationFileReader,
            IOwmApiReader owmApiReader, IFileStorageImpl fileStorageImpl,
            IOwmToElasticDocumentConverter owmToElasticDocumentConverter)
        {
            _elasticConnection = elasticConnection;
            _locationFileReader = locationFileReader;
            _owmApiReader = owmApiReader;
            _fileStorage = fileStorageImpl;
            _owmConverter = owmToElasticDocumentConverter;
        }

        public async Task<Option<WeatherLocationDocument>> ConvertAsync(Root owmDoc) =>
            await _owmConverter.ConvertAsync(owmDoc);

        public async Task WriteAllDataAsync<T>(IEnumerable<T> roots) => await _fileStorage.WriteAllDataAsync(roots);

        public async Task<Option<string>> ReadDataFromLocationAsync(string url) =>
            await _owmApiReader.ReadDataFromLocationAsync(url);

        public async Task<Option<List<string>>> ReadConfigurationAsync() =>
            await _locationFileReader.ReadConfigurationAsync();

        public async Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : WeatherLocationDocument => await _elasticConnection.BulkWriteDocumentsAsync(documents, indexName);

        public async Task<bool> DeleteIndexAsync(string indexName) =>
            await _elasticConnection.DeleteIndexAsync(indexName);

        public async Task<bool> IndexExistsAsync(string indexName) =>
            await _elasticConnection.IndexExistsAsync(indexName);

        public async Task<bool> CreateIndexAsync(string indexName) =>
            await _elasticConnection.CreateIndexAsync(indexName);

        public abstract Task Process(ConfigurationItems configuration);





    }
}