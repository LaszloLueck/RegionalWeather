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

            var mainTask = (from configuration in new ConfigurationBuilder(configurationFactory).GetConfiguration()
                from locations in new LocationFileReader().Build(configuration).ReadConfiguration()
                select new Tuple<ConfigurationItems, List<string>>(configuration, locations)).Map(tpl =>
            {
                Task.Run(async () => {
                    await Log.InfoAsync("Build up the scheduler");
                    ISchedulerFactory schedulerFactory =
                        new CustomSchedulerFactory<SchedulerJob>("job1", "group1", "trigger1", tpl.Item1, tpl.Item2);
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