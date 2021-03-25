using System;
using System.Threading.Tasks;
using Optional.Linq;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;
using RegionalWeather.Scheduler;

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
            var mainTask = (await new ConfigurationBuilder(configurationFactory).GetConfigurationAsync()).Select(
                configuration =>
                {
                    Task.Run(() =>
                    {
                        SerilogLoggerFactory.BuildLogger(configuration).Map(async serilogLogger =>
                        {
                            var logForThisClass = serilogLogger.ForContext<Program>();
                            logForThisClass.Information("Build up the scheduler");

                            ISchedulerFactory currentWeatherSchedulerFactory =
                                new CustomSchedulerFactory<CurrentWeatherSchedulerJob>("currentWeatherJob",
                                    "currentWeatherGroup", "currentWeatherTrigger", 10, configuration.RunsEvery,
                                    configuration, serilogLogger);
                            ISchedulerFactory currentWeatherReindexerFactory =
                                new CustomSchedulerFactory<ReindexerSchedulerJobWeather>("reIndexerJob", "reIndexerGroup",
                                    "reIndexerTrigger", 5, configuration.ReindexLookupEvery, configuration, serilogLogger);

                            ISchedulerFactory airPollutionSchedulerFactory =
                                new CustomSchedulerFactory<AirPollutionSchedulerJob>("airPollutionJob", "airPollutionGroup",
                                    "airPollutionTrigger", 15, configuration.AirPollutionRunsEvery, configuration, serilogLogger);

                            ISchedulerFactory airPollutionReindexerFactory =
                                new CustomSchedulerFactory<ReindexerSchedulerJobAirPollution>("reindexerAirPollutionJob",
                                    "reindexerAirPollutionGroup", "reindexerAirPollutionTrigger", 15,
                                    configuration.ReindexLookupEvery, configuration, serilogLogger);

                            await currentWeatherSchedulerFactory.RunScheduler();
                            await currentWeatherReindexerFactory.RunScheduler();
                            await airPollutionSchedulerFactory.RunScheduler();
                            await airPollutionReindexerFactory.RunScheduler();
                            logForThisClass.Information("App is in running state!");
                        });
                    });
                    return Task.Delay(-1);
                }).ValueOr(() => Task.CompletedTask);

            await Task.WhenAll(mainTask);
            Environment.Exit(1);
        }
    }
}