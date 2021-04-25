using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RegionalWeather.Logging;
using Serilog;

namespace RegionalWeather.Filestorage
{
    public interface IFileStorage
    {
        public Task WriteAllDataAsync<T>(IEnumerable<T> roots, string fileName);

        public string GetAbsoluteFilePath(string relativePath);

        public string GetAbsoluteDirectoryPath(string path);

        public bool DirectoryExists(string path);

        public bool CreateDirectory(string path);
    }

    public class FileStorageImpl : IFileStorage
    {
        private readonly ILogger _logger;

        public FileStorageImpl(ILogger loggingBase)
        {
            _logger = loggingBase.ForContext<FileStorageImpl>();
        }

        public string GetAbsoluteFilePath(string relativePath)
        {
            return Path.GetFullPath(relativePath);
        }

        public bool CreateDirectory(string path)
        {
            var info = Directory.CreateDirectory(path);
            return info.Exists;
        }

        public string GetAbsoluteDirectoryPath(string path)
        {
            return Path.GetDirectoryName(path);
        }

        public bool DirectoryExists(string path)
        {
            return Directory.Exists(path);
        }

        public async Task WriteAllDataAsync<T>(IEnumerable<T> roots, string fileName)
        {
            var sw = Stopwatch.StartNew();
            try
            {
                await Task.Run(async () =>
                {
                    _logger.Information($"write entries of type {typeof(T)} to file {fileName}");
                    await using StreamWriter streamWriter = new StreamWriter(fileName, true, Encoding.UTF8, 32768);
                    var enumerable = roots.ToList();
                    enumerable.ToList().ForEach(async root =>
                    {
                        await using MemoryStream str = new MemoryStream();
                        await JsonSerializer.SerializeAsync(str, root, root.GetType());
                        str.Position = 0;
                        await streamWriter.WriteLineAsync(await new StreamReader(str).ReadToEndAsync());
                    });
                    _logger.Information($"Successfully written {enumerable.Count} lines to file");
                    await streamWriter.FlushAsync();
                    await streamWriter.DisposeAsync();
                });
            }
            finally
            {
                sw.Stop();
                _logger.Information("Processed {MethodName} in {ElapsedMs:000} ms", $"WriteAllDataAsync<{typeof(T)}>",
                    sw.ElapsedMilliseconds);
            }
        }
    }
}