namespace SearchEngine.Shared;

/// <summary>
/// Helper for the document information to be passed between subprojects.
/// </summary>
public class SearchDocument
{
    /// <summary>
    /// The title of the document.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The summary/abstract of the document.
    /// </summary>
    public string? Summary { get; set; }
    
    /// <summary>
    /// The arXiv ID of the document.
    /// </summary>
    public string? ArXivId { get; set; }
    
    /// <summary>
    /// The vector database index ID.
    /// </summary>
    public ulong? IndexId { get; set; }
    
    /// <summary>
    /// The authors of the document.
    /// </summary>
    public string[]? Authors { get; set; }
    
    /// <summary>
    /// The date and time this document was updated.
    /// </summary>
    public DateTime? Updated { get; set; }
    
    /// <summary>
    /// The categories of the documents.
    /// </summary>
    public string[]? Categories { get; set; }
    
    /// <summary>
    /// Other documents which link to this document.
    /// </summary>
    public string[]? Linked { get; set; }

    /// <summary>
    /// Write this to a standardized format.
    /// </summary>
    /// <returns>The formatted string.</returns>
    public override string ToString()
    {
        return Format();
    }

    /// <summary>
    /// Write to a standardized format, optionally ignoring some parts for clarity.
    /// </summary>
    /// <param name="id">If the ID should be in the string.</param>
    /// <param name="authors">If the authors should be in the string.</param>
    /// <param name="updated">If when the document was updated should be in the string.</param>
    /// <returns>The formatted string.</returns>
    public string Format(bool id = false, bool authors = false, bool updated = false)
    {
        // Add the ID if we should.
        string result;
        if (id)
        {
            result = id && ArXivId != null ? $"ArXiV ID: {ArXivId}" : string.Empty;
            if (IndexId != null)
            {
                if (result != string.Empty)
                {
                    result += "\n";
                }
                
                result += $"Index ID: {IndexId}";
            }
        }
        else
        {
            result = string.Empty;
        }

        // Add the title if it exists.
        if (Title != null)
        {
            if (result != string.Empty)
            {
                result += "\n";
            }
            
            result += $"Title: {Title}";
        }

        // Add the authors if we should.
        if (authors && Authors != null)
        {
            if (result != string.Empty)
            {
                result += "\n";
            }

            result += $"Authors: {FormatAuthors()}";
        }

        // Add the time it was updated if we should.
        if (updated && Updated != null)
        {
            if (result != string.Empty)
            {
                result += "\n";
            }
            
            result += $"Updated: {FormatUpdated()}";
        }

        // Add the summary if it exists.
        if (Summary == null)
        {
            return result;
        }

        if (result != string.Empty)
        {
            result += "\n";
        }

        return result + $"Abstract: {Summary}";
    }
    
    /// <summary>
    /// Format the authors properly.
    /// </summary>
    /// <param name="html">If this is for HTML and thus an "et al." should be italicized.</param>
    /// <returns>The authors formatted properly.</returns>
    public string FormatAuthors(bool html = false)
    {
        // If there are no authors, there is nothing to return.
        if (Authors == null || Authors.Length == 0)
        {
            return string.Empty;
        }

        switch (Authors.Length)
        {
            // If only one author, return them.
            case 1:
                return Authors[0];
            // For two authors, combine them with an and.
            case 2:
                return $"{Authors[0]} and {Authors[1]}";
            // For three or more authors, combine them with commas with an and for the last one.
            default:
            {
                // In IEEE, we can use "et al." if there are more than six authors.
                if (Authors.Length > 6)
                {
                    return html ? $"{Authors[0]} <i>et al.</i>" : $"{Authors[0]} et al.";
                }
                
                string allButLast = string.Join(", ", Authors.Take(Authors.Length - 1));
                return $"{allButLast}, and {Authors[^1]}";
            }
        }
    }

    /// <summary>
    /// Get the date and time formatted.
    /// </summary>
    /// <returns>The date and time formatted.</returns>
    public string FormatUpdated()
    {
        return Updated.HasValue ? Updated.Value.ToString("MMMM d, yyyy 'at' h:mm:ss tt") : string.Empty;
    }
}