using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Optional.Linq;
using RegionalWeather.Configuration;
using RegionalWeather.FileRead;
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

            var mainTask = (await new ConfigurationBuilder(configurationFactory).GetConfigurationAsync()).Select(configuration =>
            {
                Task.Run(async () => {
                    await Log.InfoAsync("Build up the scheduler");
                    ISchedulerFactory schedulerFactory =
                        new CustomSchedulerFactory<SchedulerJob>("job1", "group1", "trigger1", configuration);

                    ISchedulerFactory reindexerFactory =
                        new ReindexerFactory<ReindexerJob>("reindexJob", "reindexGroup", "reindexTrigger", configuration);

                    await reindexerFactory.RunScheduler();
                    await schedulerFactory.RunScheduler();
                    await Log.InfoAsync("App is in running state!");
                });
                return Task.Delay(-1);
            }).ValueOr(() => Task.CompletedTask);

            await Task.WhenAll(mainTask);
            Environment.Exit(1);
        }
    }
}