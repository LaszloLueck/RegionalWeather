using System.Threading.Tasks;
using RegionalWeather.Configuration;

namespace RegionalWeather.Processing
{
    public interface IProcessingBase
    {
        Task Process(ConfigurationItems configuration);

    }
}