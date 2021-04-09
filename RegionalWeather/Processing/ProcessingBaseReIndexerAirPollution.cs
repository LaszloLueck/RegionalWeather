﻿using System.Threading.Tasks;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.Owm.AirPollution;
using RegionalWeather.Reindexing;
using RegionalWeather.Transport.Elastic;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBaseReIndexerAirPollution : ProcessingBaseReIndexerProxy<AirPollutionBase>,
        IProcessingBase
    {
        protected ProcessingBaseReIndexerAirPollution(IElasticConnection elasticConnection,
            IOwmToElasticDocumentConverter<AirPollutionBase> owmDocumentConverter, IDirectoryUtils directoryUtils,
            IProcessingBaseImplementations processingBaseImplementations) : base(elasticConnection,
            processingBaseImplementations, owmDocumentConverter, directoryUtils)
        {
        }

        public abstract Task Process(ConfigurationItems configuration);
    }
}