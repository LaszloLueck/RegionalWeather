using System.Threading.Tasks;

namespace RegionalWeather.Scheduler
{
    public interface ISchedulerFactory
    {
        Task ShutdownScheduler();

        Task RunScheduler();
    }
}