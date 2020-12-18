using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly string _storageFilePath;
        public FileStorageImpl(ConfigurationItems configurationItems)
        {
            var filename = configurationItems.FileStorageTemplate.Replace("[CURRENTDATE]", DateTime.Now.ToString("yyyyMMdd"));
            _storageFilePath = filename;
        }

        public async Task WriteAllDataAsync(IEnumerable<Root> roots)
        {
            await Task.Run(async () =>
            {
                await using StreamWriter streamWriter = new StreamWriter(_storageFilePath, true, Encoding.UTF8, 8192);
                var enumerable = roots.ToList();
                enumerable.ToList().ForEach(async root =>
                {
                    await using MemoryStream str = new MemoryStream();
                    await JsonSerializer.SerializeAsync(str, root, root.GetType());
                    str.Position = 0;
                    await streamWriter.WriteLineAsync(await new StreamReader(str).ReadToEndAsync());
                });
                await Log.InfoAsync($"Successfully write {enumerable.Count} lines to file");
                await streamWriter.FlushAsync();
                await streamWriter.DisposeAsync();
            });
        }
    }
}