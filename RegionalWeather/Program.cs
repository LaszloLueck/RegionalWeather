using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using Optional;
using Optional.Linq;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;
using RegionalWeather.Scheduler;
using Serilog;

namespace RegionalWeather
{
    class Program
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<Program>.GetLogger();

        protected Program()
        {
        }

        static async Task Main(string[] args)
        {
            await Log.InfoAsync("starting app");

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
                    logForThisClass.Information($"Main :: {sw.ElapsedMilliseconds} ms");
                }

            }).ValueOr(() => Task.CompletedTask);


            await Task.WhenAll(mainTask);
            Environment.Exit(1);
        }
    }
}