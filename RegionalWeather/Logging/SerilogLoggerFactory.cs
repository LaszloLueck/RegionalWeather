using Optional;
using RegionalWeather.Configuration;
using Serilog;
using Serilog.Core;

namespace RegionalWeather.Logging
{
    public class SerilogLoggerFactory
    {
        private static Logger _logger;
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<SerilogLoggerFactory>.GetLogger();

        public static Option<ILogger> BuildLogger<T>(IConfigurationFactory configurationFactory)
        {
            
        }
        
    }
}