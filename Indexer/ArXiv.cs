using System.Text.RegularExpressions;
using System.Xml.Linq;
using SearchEngine.Lucene;
using SearchEngine.Shared;

namespace Indexer;

/// <summary>
/// Handle everything related to arXiv.
/// </summary>
public static partial class ArXiv
{
    /// <summary>
    /// The core XML for parsing.
    /// </summary>
    private const string XmlCore = "{http://www.w3.org/2005/Atom}";

    /// <summary>
    /// The default category to search in.
    /// </summary>
    private const string Category = "cs.AI";

    /// <summary>
    /// The default sorting method.
    /// </summary>
    private const string SortBy = "lastUpdatedDate";
    
    /// <summary>
    /// The default sorting order.
    /// </summary>
    private const string SortOrder = "descending";

    /// <summary>
    /// The default maximum number of results to get from arXiv at once.
    /// </summary>
    private const int MaxResults = 100;

    /// <summary>
    /// The default total number of results we want for our own database.
    /// </summary>
    private const int TotalResults = 100;
    
    /// <summary>
    /// Regex method to remove all whitespace.
    /// </summary>
    /// <returns>The regex method to remove all whitespace.</returns>
    [GeneratedRegex(@"\s+")]
    private static partial Regex CleanWhitespaceRegex();

    /// <summary>
    /// Regex method to remove illegal characters for file names.
    /// </summary>
    /// <returns>The regex method to remove all illegal characters for file names.</returns>
    [GeneratedRegex(@"[\\/:*?""<>|]")]
    private static partial Regex CleanFileName();

    /// <summary>
    /// Save raw documents from arXiv.
    /// </summary>
    /// <param name="category">The category to search in.</param>
    /// <param name="sortBy">The sorting method - lastUpdatedDate relevance submittedDate.</param>
    /// <param name="sortOrder">The sorting order - descending ascending.</param>
    /// <param name="maxResults">The maximum number of results to get from arXiv at once.</param>
    /// <param name="totalResults">The total number of results we want for our own database.</param>
    public static async Task SaveDocumentsGetLinksAsync(string category = Category, string sortBy = SortBy, string sortOrder = SortOrder, int maxResults = MaxResults, int totalResults = TotalResults)
    {
        // Ensure valid values.
        maxResults = maxResults switch
        {
            < 1 => 1,
            > 1000 => 1000,
            _ => maxResults
        };
        
        totalResults = totalResults switch
        {
            < 1 => 1,
            > 1000 => 1000,
            _ => totalResults
        };
        
        
        // Ensure the directory to save raw files exists.
        string directoryPath = Core.GetFilePath(Core.GetDataset);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // See how many documents already exist.
        HashSet<string> files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories).ToHashSet();
        totalResults -= files.Count;
        
        // Query until we have enough documents for our dataset.
        int startIndex = 0;
        while (totalResults > 0)
        {
            // Search for more documents.
            List<SearchDocument> documents = await GetLinksAsync(category, sortBy, sortOrder, maxResults, startIndex);
            
            // Try every document.
            foreach (SearchDocument document in documents)
            {
                // If there was no title (should never get to here), do nothing.
                if (document.Title == null)
                {
                    continue;
                }

                // If the file already exists, there is no need to write it again.
                string path = Path.Combine(directoryPath, $"{CleanFileName().Replace(document.Title, string.Empty)}.txt");
                if (files.Contains(path))
                {
                    continue;
                }

                // Write to the new file.
                string contents = $"{document.Url}\n{document.Title}\n{document.Summary}";
                if (document.Authors != null)
                {
                    contents = document.Authors.Aggregate(contents, (current, author) => current + $"\n{author}");
                }
                
                await File.WriteAllTextAsync(path, contents);
                
                // If we have enough documents, stop.
                if (--totalResults <= 0)
                {
                    return;
                }
            }

            // For the next query, index the next possible documents.
            startIndex += maxResults;
        }
    }
    
    /// <summary>
    /// Get documents from arXiv.
    /// </summary>
    /// <param name="category">The category to search in.</param>
    /// <param name="sortBy">The sorting method - lastUpdatedDate relevance submittedDate.</param>
    /// <param name="sortOrder">The sorting order - descending ascending.</param>
    /// <param name="maxResults">The maximum number of results to get from arXiv at once.</param>
    /// <param name="startIndex">The index to start requests from at arXiv.</param>
    public static async Task<List<SearchDocument>> GetLinksAsync(string category = Category, string sortBy = SortBy, string sortOrder = SortOrder, int maxResults = MaxResults, int startIndex = 0)
    {
        // Ensure valid values.
        maxResults = maxResults switch
        {
            < 1 => 1,
            > 1000 => 1000,
            _ => maxResults
        };

        if (startIndex < 0)
        {
            startIndex = 0;
        }

        // Make the HTTP GET request.
        HttpResponseMessage response = await new HttpClient().GetAsync($"http://export.arxiv.org/api/query?search_query=cat:{category}&start={startIndex}&max_results={maxResults}&sortBy={sortBy}&sortOrder={sortOrder}");

        // Stop if there is an error.
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed: {response.StatusCode}");
        }

        // Save our documents.
        List<SearchDocument> documents = [];
        foreach (XElement entry in XDocument.Parse(await response.Content.ReadAsStringAsync()).Descendants($"{XmlCore}entry"))
        {
            // If any value is empty, skip this document.
            string? url = CleanElement(entry, "id");
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }
            
            string? title = CleanElement(entry, "title");
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }
            
            string? summary = CleanElement(entry, "summary");
            if (string.IsNullOrWhiteSpace(summary))
            {
                continue;
            }
            
            // Add the document.
            documents.Add(new()
            {
                Url = url,
                Title = title,
                Summary = summary,
                Authors = GetAuthors(entry)
            });
        }
        
        return documents;
    }
    
    /// <summary>
    /// Get the authors from the XML.
    /// </summary>
    /// <param name="entry">The XML entry.</param>
    /// <returns>The authors as a list.</returns>
    private static string[] GetAuthors(XElement entry)
    {
        return entry
            .Elements($"{XmlCore}author")
            .Select(author => CleanString(author.Element($"{XmlCore}name")?.Value ?? string.Empty))
            .ToArray();
    }

    /// <summary>
    /// Get an element from the XML.
    /// </summary>
    /// <param name="entry">The XML entry.</param>
    /// <param name="element">The element to get.</param>
    /// <returns>The element if it was found, otherwise null.</returns>
    private static string? CleanElement(XElement entry, string element)
    {
        XElement? linkElement = entry.Element($"{XmlCore}{element}");
        return linkElement != null ? CleanString(linkElement.Value) : null;
    }

    /// <summary>
    /// Clean our strings.
    /// </summary>
    /// <param name="s">The string to clean.</param>
    /// <returns>The string with all whitespaces replaced by spaces and trimmed.</returns>
    private static string CleanString(string s)
    {
        return CleanWhitespaceRegex().Replace(s, " ").Trim();
    }
}