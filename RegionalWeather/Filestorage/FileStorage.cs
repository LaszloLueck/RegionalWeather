using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RegionalWeather.Configuration;
using RegionalWeather.Logging;
using RegionalWeather.Owm;

namespace RegionalWeather.Filestorage
{
    interface IFileStorage
    {
        FileStorageImpl Build(ConfigurationItems configurationItems);
    }

    public class FileStorage : IFileStorage
    {
        public FileStorageImpl Build(ConfigurationItems configurationItems)
        {
            return new(configurationItems);
        }
    }

    public class FileStorageImpl
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<FileStorageImpl>.GetLogger();
        private readonly StreamWriter _sw;
        public FileStorageImpl(ConfigurationItems configurationItems)
        {
            var filename = configurationItems.FileStorageTemplate.Replace("[CURRENTDATE]", DateTime.Now.ToString("yyyyMMdd"));
            _sw = new StreamWriter(filename, true);
        }

        public async void FlushDataAsync()
        {
            await _sw.FlushAsync();
        }
        
        public async void CloseFileStreamAsync()
        {
            _sw.Close();
            await _sw.DisposeAsync();
        }
        

        public async Task WriteDataAsync(Root sourceObject)
        {
            try
            {
                sourceObject.ReadTime = DateTime.Now;
                var str = new MemoryStream();
                await JsonSerializer.SerializeAsync(str, sourceObject, sourceObject.GetType());
                str.Position = 0;
                await _sw.WriteLineAsync(await new StreamReader(str).ReadToEndAsync());
            }
            catch (Exception exception)
            {
                await Log.ErrorAsync(exception, "Error while writing data to file!");
                await Task.Run(() => sourceObject);
            }
        }
    }
}