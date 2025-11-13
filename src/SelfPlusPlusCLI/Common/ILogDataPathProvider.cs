namespace SelfPlusPlusCLI.Common;

public interface ILogDataPathProvider
{
    string GetLogDataDirectory();
    string GetLogDataFilePath();
}

