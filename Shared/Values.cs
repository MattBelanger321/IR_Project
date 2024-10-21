namespace SearchEngine.Shared;

/// <summary>
/// Common values across multiple projects.
/// </summary>
public static class Values
{
    /// <summary>
    /// The name of the dataset.
    /// </summary>
    public const string Dataset = "arXiv";

    /// <summary>
    /// The extended path for summaries.
    /// </summary>
    public const string Summaries = "_summaries";
    
    /// <summary>
    /// How many search results a query should return.
    /// </summary>
    public const int SearchCount = 10;

    /// <summary>
    /// Get the path to a file.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>The path to the file.</returns>
    public static string GetFilePath(string fileName)
    {
        return Path.Combine(GetRootDirectory() ?? string.Empty, fileName);
    }
    
    /// <summary>
    /// Get the root directory of the project.
    /// </summary>
    /// <returns></returns>
    public static string? GetRootDirectory()
    {
        // Traverse upwards to find the project root, as .NET projects are nested deeper with how they run.
        return Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName;
    }
    
    /// <summary>
    /// Get the dataset directory.
    /// </summary>
    public static string GetDataset => Path.Combine(GetRootDirectory() ?? string.Empty, Dataset);
}