using System.Collections.Generic;
using System.Threading.Tasks;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseReIndexer : ProcessingBaseImplementations, IDirectoryUtils, IProcessingBase
    {

        protected readonly ElasticConnection ElasticConnectionImpl;
        protected readonly OwmToElasticDocumentConverter OwmToElasticDocumentConverterImpl;
        protected readonly IDirectoryUtils DirectoryUtilsImpl;
        
        
        protected ProcessingBaseReIndexer(ElasticConnection elasticConnection, OwmToElasticDocumentConverter owmToElasticDocumentConverter, IDirectoryUtils directoryUtilsImpl) : base(elasticConnection)
        {
            ElasticConnectionImpl = elasticConnection;
            OwmToElasticDocumentConverterImpl = owmToElasticDocumentConverter;
            DirectoryUtilsImpl = directoryUtilsImpl;
        }


        public bool DirectoryExists(string path) => DirectoryUtilsImpl.DirectoryExists(path);

        public bool CreateDirectory(string path) => DirectoryUtilsImpl.CreateDirectory(path);

        public IEnumerable<string> GetFilesOfDirectory(string path, string filePattern) =>
            DirectoryUtilsImpl.GetFilesOfDirectory(path, filePattern);

        public async Task<IEnumerable<string>> ReadAllLinesOfFileAsync(string path) =>
            await DirectoryUtilsImpl.ReadAllLinesOfFileAsync(path);

        public bool DeleteFile(string path) => DirectoryUtilsImpl.DeleteFile(path);
            
        public abstract Task Process(ConfigurationItems configuration);

    }
}