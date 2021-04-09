using System.Collections.Generic;
using System.Threading.Tasks;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseReIndexerWeather : ProcessingBaseReIndexerProxy<CurrentWeatherBase>,
        IDirectoryUtils, IProcessingBase
    {
        private readonly IDirectoryUtils _directoryUtils;

        protected ProcessingBaseReIndexerWeather(IElasticConnection elasticConnection,
            IOwmToElasticDocumentConverter<CurrentWeatherBase> owmDocumentConverter, IDirectoryUtils directoryUtils,
            IProcessingBaseImplementations processingBaseImplementations) : base(elasticConnection,
            processingBaseImplementations, owmDocumentConverter)
        {
            _directoryUtils = directoryUtils;
        }

        public IEnumerable<string> ReadAllLinesOfFile(string path) => _directoryUtils.ReadAllLinesOfFile(path);

        public bool DirectoryExists(string path) => _directoryUtils.DirectoryExists(path);

        public bool CreateDirectory(string path) => _directoryUtils.CreateDirectory(path);

        public IEnumerable<string> GetFilesOfDirectory(string path, string filePattern) =>
            _directoryUtils.GetFilesOfDirectory(path, filePattern);

        public bool DeleteFile(string path) => _directoryUtils.DeleteFile(path);

        public abstract Task Process(ConfigurationItems configuration);
    }
}