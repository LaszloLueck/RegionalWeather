#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RegionalWeather.Configuration;
using RegionalWeather.Elastic;
using RegionalWeather.FileRead;
using RegionalWeather.Filestorage;
using RegionalWeather.Transport.Elastic;
using RegionalWeather.Transport.Owm;

namespace RegionalWeather.Processing
{
    public abstract class ProcessingBase : ProcessingBaseImplementations, IProcessingBase

    {
    protected readonly ElasticConnection ElasticConnectionImpl;
    protected readonly LocationFileReaderImpl LocationFileReader;
    protected readonly OwmApiReader OwmApiReaderImpl;
    protected readonly FileStorageImpl FileStorage;
    protected readonly OwmToElasticDocumentConverter OwmConverter;


    protected ProcessingBase(ElasticConnection elasticConnection, LocationFileReaderImpl locationFileReader,
        OwmApiReader owmApiReader, FileStorageImpl fileStorageImpl,
        OwmToElasticDocumentConverter owmToElasticDocumentConverter) : base(elasticConnection)
    {
        ElasticConnectionImpl = elasticConnection;
        LocationFileReader = locationFileReader;
        OwmApiReaderImpl = owmApiReader;
        FileStorage = fileStorageImpl;
        OwmConverter = owmToElasticDocumentConverter;
    }

    public abstract Task Process(ConfigurationItems configuration);





    }
}