using SelfPlusPlusCLI.Common;

namespace SelfPlusPlusCLI.Tests.Support;

internal sealed class TempLogDataPathProvider : ILogDataPathProvider, IDisposable
{
    private readonly string _directory;
    private readonly string _filePath;

    public TempLogDataPathProvider()
    {
        _directory = Path.Combine(Path.GetTempPath(), "SelfPlusPlusTests", Guid.NewGuid().ToString("N"));
        _filePath = Path.Combine(_directory, DefaultLogDataPathProvider.LogDataFileName);
    }

    public string Directory => _directory;

    public string FilePath => _filePath;

    public string GetLogDataDirectory()
    {
        return _directory;
    }

    public string GetLogDataFilePath()
    {
        return _filePath;
    }

    public void Dispose()
    {
        try
        {
            if (System.IO.Directory.Exists(_directory))
            {
                System.IO.Directory.Delete(_directory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests.
        }
    }
}

