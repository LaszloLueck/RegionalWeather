using System;
using System.Threading.Tasks;
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
            await Log.InfoAsync("Load and build the configuration");

            IConfigurationFactory configurationFactory = new ConfigurationFactory();
            var mainTask = new ConfigurationBuilder(configurationFactory).GetConfiguration().Map(configuration => {
                Task.Run(async () => {
                    await Log.InfoAsync("successfully loaded configuration");
                    await Log.InfoAsync("Build up the scheduler");
                    ISchedulerFactory schedulerFactory =
                        new CustomSchedulerFactory<SchedulerJob>("job1", "group1", "trigger1", configuration);
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