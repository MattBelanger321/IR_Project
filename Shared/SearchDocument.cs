namespace SearchEngine.Shared;

/// <summary>
/// Helper for the document information to be passed between sub-projects.
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
    /// The URL of the document.
    /// </summary>
    public string? Url { get; set; }
    
    /// <summary>
    /// The authors of the document.
    /// </summary>
    public string[]? Authors { get; set; }

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
    /// <param name="url">If the URL should be in the string.</param>
    /// <param name="authors">If the authors should be in the string.</param>
    /// <param name="html">If this is for HTML and thus an "et al." should be italicized.</param>
    /// <returns>The formatted string.</returns>
    public string Format(bool url = false, bool authors = false, bool html = false)
    {
        // Add the URL if we should.
        string result = url && Url != null ? $"URL: {Url}" : string.Empty;

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

            result += $"Authors: {FormatAuthors(html)}";
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
}