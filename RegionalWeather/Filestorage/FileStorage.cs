using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RegionalWeather.Logging;

namespace RegionalWeather.Filestorage
{
    interface IFileStorage
    {
        FileStorageImpl Build();
    }

    public class FileStorage : IFileStorage
    {
        public FileStorageImpl Build()
        {
            return new();
        }
    }

    public interface IFileStorageImpl
    {
        public Task WriteAllDataAsync<T>(IEnumerable<T> roots, string fileName);
    }

    public class FileStorageImpl : IFileStorageImpl
    {
        private static readonly IMySimpleLogger Log = MySimpleLoggerImpl<FileStorageImpl>.GetLogger();

        public async Task WriteAllDataAsync<T>(IEnumerable<T> roots, string fileName)
        {
            await Task.Run(async () =>
            {
                await using StreamWriter streamWriter = new StreamWriter(fileName, true, Encoding.UTF8, 32768);
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