using System.Collections.Generic;

namespace RegionalWeather.Reindexing
{
    public interface IDirectoryUtils
    {
        bool DirectoryExists(string path);

        bool CreateDirectory(string path);

        IEnumerable<string> GetFilesOfDirectory(string path, string filePattern);

        IEnumerable<string> ReadAllLinesOfFile(string path);

        bool DeleteFile(string path);

    }
}