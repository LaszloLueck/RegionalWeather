using System;
using System.Linq;
using Optional;
using RegionalWeather.Configuration;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Elasticsearch;

namespace RegionalWeather.Logging
{
    public class SerilogLoggerFactory
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<SerilogLoggerFactory>.GetLogger();

        protected SerilogLoggerFactory(){}
     
        public static Option<Logger> BuildLogger(ConfigurationItems configurationItems)
        {
            try
            {
                Log.Info("build the Serilog logger factory");
                var elasticUriList = configurationItems.ElasticHostsAndPorts.Split(",").Select(uri => new Uri(uri));

                var l = configurationItems.LogToElasticSearch
                    ? new LoggerConfiguration().WriteTo.Elasticsearch(new ElasticsearchSinkOptions(elasticUriList)
                    {
                        AutoRegisterTemplate = true, AutoRegisterTemplateVersion = AutoRegisterTemplateVersion.ESv7,
                        IndexFormat = $"{configurationItems.ElasticSearchLogIndexName}-{DateTime.UtcNow:yyyy-MM}"
                    })
                    : new LoggerConfiguration();

                //add console logging
                var finishedLogger = l
                    .WriteTo
                    .ColoredConsole(
                        outputTemplate:
                        "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level}] ({SourceContext:1}.{Method}) {Message}{NewLine}{Exception}")
                    .Enrich
                    .FromLogContext()
                    .CreateLogger();

                return Option.Some(finishedLogger);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "An error occured while building the Serilog logger factory");
                return Option.None<Logger>();
            }
        }
    }
}