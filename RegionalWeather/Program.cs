using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Optional.Linq;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;
using RegionalWeather.Scheduler;
using Serilog;
using Serilog.Enrichers.WithCaller;
using Serilog.Sinks.SystemConsole.Themes;

namespace RegionalWeather
{
    class Program
    {
        protected Program()
        {
        }

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.Console(theme: AnsiConsoleTheme.Code,
                    outputTemplate: "[{Timestamp:yyy-MM-dd HH:mm:ss} {Level:u4}] {Caller}{NewLine}{Message:lj}{NewLine}{Exception}")
                .Enrich.FromLogContext()
                .Enrich.WithCaller()
                .CreateLogger();

            Log.Information("starting app");

            IConfigurationFactory configurationFactory = new ConfigurationFactory();

            var startupObjectOpt =
                from configuration in await new ConfigurationBuilder(configurationFactory).GetConfigurationAsync()
                from seriLogger in SerilogLoggerFactory.BuildLogger(configuration)
                select new Tuple<ConfigurationItems, ILogger>(configuration, seriLogger);

            var mainTask = startupObjectOpt.Map(tpl =>
            {
                var configuration = tpl.Item1;
                var serilogLogger = tpl.Item2;
                var logForThisClass = serilogLogger.ForContext<Program>();
                var sw = Stopwatch.StartNew();
                logForThisClass.Information("Build up the scheduler");
                try
                {
                    Task.Run(async () =>
                    {
                        ISchedulerFactory currentWeatherSchedulerFactory =
                            new CustomSchedulerFactory<CurrentWeatherSchedulerJob>("currentWeatherJob",
                                "currentWeatherGroup", "currentWeatherTrigger", 10, configuration.RunsEvery,
                                configuration, serilogLogger);
                        ISchedulerFactory currentWeatherReindexerFactory =
                            new CustomSchedulerFactory<ReindexerSchedulerJobWeather>("reIndexerJob",
                                "reIndexerGroup",
                                "reIndexerTrigger", 5, configuration.ReindexLookupEvery, configuration,
                                serilogLogger);

                        ISchedulerFactory airPollutionSchedulerFactory =
                            new CustomSchedulerFactory<AirPollutionSchedulerJob>("airPollutionJob",
                                "airPollutionGroup",
                                "airPollutionTrigger", 15, configuration.AirPollutionRunsEvery, configuration,
                                serilogLogger);

                        ISchedulerFactory airPollutionReindexerFactory =
                            new CustomSchedulerFactory<ReindexerSchedulerJobAirPollution>(
                                "reindexerAirPollutionJob",
                                "reindexerAirPollutionGroup", "reindexerAirPollutionTrigger", 15,
                                configuration.ReindexLookupEvery, configuration, serilogLogger);

                        await currentWeatherSchedulerFactory.RunScheduler();
                        await currentWeatherReindexerFactory.RunScheduler();
                        await airPollutionSchedulerFactory.RunScheduler();
                        await airPollutionReindexerFactory.RunScheduler();
                        logForThisClass.Information("App is in running state!");
                    });
                    return Task.Delay(-1);
                }
                finally
                {
                    sw.Stop();
                    logForThisClass.Information("Processed {MethodName} in {ElapsedMs:000} ms", "Main",
                        sw.ElapsedMilliseconds);
                }
            }).ValueOr(() => Task.CompletedTask);


            await Task.WhenAll(mainTask);
            Environment.Exit(1);
        }
    }
}