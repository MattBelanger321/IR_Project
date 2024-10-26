namespace SearchEngine.Shared;

/// <summary>
/// Used to store information about a query.
/// </summary>
public class QueryResult
{
    /// <summary>
    /// If spelling correction was done, this is the corrected query.
    /// </summary>
    public string? CorrectedQuery { get; set; }
    
    /// <summary>
    /// The documents returned from the query.
    /// </summary>
    public List<SearchDocument> SearchDocuments { get; set; } = [];
}