using System.Collections.Generic;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseAirPollution : ProcessingBaseImplementations, IElasticConnection,
        ILocationFileReaderImpl, IProcessingBase, IOwmApiReader, IFileStorageImpl, IOwmToElasticDocumentConverter<AirPollutionBase>
    {
        private readonly IElasticConnection _elasticConnection;
        private readonly ILocationFileReaderImpl _locationFileReaderImpl;
        private readonly IFileStorageImpl _fileStorageImpl;
        private readonly IOwmApiReader _owmApiReader;
        private readonly IOwmToElasticDocumentConverter<AirPollutionBase> _owmToElasticDocumentConverter;

        protected ProcessingBaseAirPollution(IElasticConnection elasticConnection,
            ILocationFileReaderImpl locationFileReaderImplImpl, IFileStorageImpl fileStorageImplImpl,
            IOwmApiReader owmApiReader, IOwmToElasticDocumentConverter<AirPollutionBase> owmToElasticDocumentConverter)
        {
            _elasticConnection = elasticConnection;
            _locationFileReaderImpl = locationFileReaderImplImpl;
            _fileStorageImpl = fileStorageImplImpl;
            _owmApiReader = owmApiReader;
            _owmToElasticDocumentConverter = owmToElasticDocumentConverter;
        }

        public async Task<Option<ElasticDocument>> ConvertAsync(AirPollutionBase owmDoc) =>
            await _owmToElasticDocumentConverter.ConvertAsync(owmDoc);

        public async Task<Option<List<string>>> ReadLocationsAsync(string locationPath) =>
            await _locationFileReaderImpl.ReadLocationsAsync(locationPath);

        public async Task WriteAllDataAsync<T>(IEnumerable<T> roots, string fileName) =>
            await _fileStorageImpl.WriteAllDataAsync(roots, fileName);

        public async Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : ElasticDocument => await _elasticConnection.BulkWriteDocumentsAsync(documents, indexName);

        public async Task<bool> IndexExistsAsync(string indexName) =>
            await _elasticConnection.IndexExistsAsync(indexName);

        public async Task<bool> CreateIndexAsync<T>(string indexName) where T : ElasticDocument =>
            await _elasticConnection.CreateIndexAsync<AirPollutionDocument>(indexName);

        public async Task<bool> DeleteIndexAsync(string indexName) =>
            await _elasticConnection.DeleteIndexAsync(indexName);

        public abstract Task Process(ConfigurationItems configuration);

        public async Task<Option<string>> ReadDataFromLocationAsync(string url) =>
            await _owmApiReader.ReadDataFromLocationAsync(url);
    }
}