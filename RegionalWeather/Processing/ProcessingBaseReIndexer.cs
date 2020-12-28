using System.Collections.Generic;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Owm;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseReIndexer : ProcessingBaseImplementations, IDirectoryUtils, IElasticConnection, IOwmToElasticDocumentConverter, IProcessingBase
    {
        private readonly IElasticConnection _elasticConnection;
        private readonly IOwmToElasticDocumentConverter _owmDocumentConverter;
        private readonly IDirectoryUtils _directoryUtils;
        
        
        protected ProcessingBaseReIndexer(IElasticConnection elasticConnection, IOwmToElasticDocumentConverter owmDocumentConverter, IDirectoryUtils directoryUtils)
        {
            _elasticConnection = elasticConnection;
            _owmDocumentConverter = owmDocumentConverter;
            _directoryUtils = directoryUtils;
        }

        public IEnumerable<string> ReadAllLinesOfFile(string path) => _directoryUtils.ReadAllLinesOfFile(path);

        public async Task<Option<WeatherLocationDocument>> ConvertAsync(Root owmDoc) =>
            await _owmDocumentConverter.ConvertAsync(owmDoc);

        public async Task BulkWriteDocumentsAsync<T>(IEnumerable<T> documents, string indexName)
            where T : WeatherLocationDocument =>
            await _elasticConnection.BulkWriteDocumentsAsync(documents, indexName);

        public async Task<bool> DeleteIndexAsync(string indexName) =>
            await _elasticConnection.DeleteIndexAsync(indexName);

        public async Task<bool> IndexExistsAsync(string indexName) =>
            await _elasticConnection.IndexExistsAsync(indexName);

        public async Task<bool> CreateIndexAsync(string indexName) =>
            await _elasticConnection.CreateIndexAsync(indexName);

        public bool DirectoryExists(string path) => _directoryUtils.DirectoryExists(path);
        
        public bool CreateDirectory(string path) => _directoryUtils.CreateDirectory(path);
        
        public IEnumerable<string> GetFilesOfDirectory(string path, string filePattern) =>
            _directoryUtils.GetFilesOfDirectory(path, filePattern);
        
        public async Task<IEnumerable<string>> ReadAllLinesOfFileAsync(string path) =>
            await _directoryUtils.ReadAllLinesOfFileAsync(path);
        
        public bool DeleteFile(string path) => _directoryUtils.DeleteFile(path);
            
        public abstract Task Process(ConfigurationItems configuration);

    }
}