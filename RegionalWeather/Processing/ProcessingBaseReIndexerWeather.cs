using System.Threading.Tasks;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Owm.CurrentWeather;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseReIndexerWeather : ProcessingBaseReIndexerProxy<CurrentWeatherBase>,
        IProcessingBase
    {
        protected ProcessingBaseReIndexerWeather(IElasticConnection elasticConnection,
            IOwmToElasticDocumentConverter<CurrentWeatherBase> owmDocumentConverter, IDirectoryUtils directoryUtils,
            IProcessingBaseImplementations processingBaseImplementations) : base(elasticConnection,
            processingBaseImplementations, owmDocumentConverter, directoryUtils)
        {
        }

        public abstract Task Process(ConfigurationItems configuration);
    }
}