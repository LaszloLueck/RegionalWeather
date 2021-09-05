using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Optional;
using RegionalWeather.Configuration;
using RegionalWeather.Scheduler;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.ShortTypeName;
using Serilog.Enrichers.WithCaller;
using Serilog.Sinks.Elasticsearch;
using Serilog.Sinks.SystemConsole.Themes;

namespace RegionalWeather
{
    internal static class BuildLoggerConfiguration
    {
        public static Logger Build()
        {
            return new LoggerConfiguration()
                .Enrich.WithShortTypeName()
                .MinimumLevel.Information()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code,
                    outputTemplate:
                    "[{Timestamp:yyy-MM-dd HH:mm:ss} {Level:u3} {ShortTypeName}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();
        }
    }

    class Program
    {
        protected Program()
        {
        }

        private static ILogger _logger;

        static async Task Main(string[] args)
        {
            Log.Logger = BuildLoggerConfiguration.Build();

            _logger = Log.Logger.ForContext<Program>();
            _logger.Information("starting app");

            IConfigurationFactory configurationFactory = new ConfigurationFactory();

            var startupObjectOpt = await new ConfigurationBuilder(configurationFactory).GetConfigurationAsync();

            var mainTask = startupObjectOpt.Map(configuration =>
            {
                var sw = Stopwatch.StartNew();
                _logger.Information("Build up the scheduler");
                try
                {
                    Task.Run(async () =>
                    {
                        ISchedulerFactory currentWeatherSchedulerFactory =
                            new CustomSchedulerFactory<CurrentWeatherSchedulerJob>("currentWeatherJob",
                                "currentWeatherGroup", "currentWeatherTrigger", 10, configuration.RunsEvery,
                                configuration);
                        ISchedulerFactory airPollutionSchedulerFactory =
                            new CustomSchedulerFactory<AirPollutionSchedulerJob>("airPollutionJob",
                                "airPollutionGroup",
                                "airPollutionTrigger", 15, configuration.AirPollutionRunsEvery, configuration);
                        await currentWeatherSchedulerFactory.RunScheduler();
                        await airPollutionSchedulerFactory.RunScheduler();
                        _logger.Information("App is in running state!");
                    });
                    return Task.Delay(-1);
                }
                finally
                {
                    sw.Stop();
                    _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", "Main",
                        sw.ElapsedMilliseconds);
                }
            }).ValueOr(() => Task.CompletedTask);


            await Task.WhenAll(mainTask);
            Environment.Exit(1);
        }
    }
}