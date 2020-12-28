using System.Collections.Generic;
using System.Threading.Tasks;

namespace RegionalWeather.Reindexing
{
    public interface IDirectoryUtils
    {
        bool DirectoryExists(string path);

        bool CreateDirectory(string path);

        IEnumerable<string> GetFilesOfDirectory(string path, string filePattern);

        IEnumerable<string> ReadAllLinesOfFile(string path);

        Task<IEnumerable<string>> ReadAllLinesOfFileAsync(string path);

        bool DeleteFile(string path);

    }
}