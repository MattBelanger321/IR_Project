using System.Text.RegularExpressions;
using System.Xml.Linq;
using Markdig;
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
    private const int MaxResults = 1000;

    /// <summary>
    /// The default total number of results we want for our own database.
    /// </summary>
    private const int TotalResults = 5000;

    /// <summary>
    /// All arXiv computer science categories.
    /// </summary>
    private static readonly string[] Categories = [
        "cs.AI", "cs.AR", "cs.CC", "cs.CE", "cs.CG", "cs.CL", "cs.CR", "cs.CV", "cs.CY", "cs.DB", "cs.DC", "cs.DL",
        "cs.DM", "cs.DS", "cs.ET", "cs.FL", "cs.GL", "cs.GR", "cs.GT", "cs.HC", "cs.IR", "cs.IT", "cs.LG", "cs.LO",
        "cs.MA", "cs.MM", "cs.MS", "cs.NA", "cs.NE", "cs.NI", "cs.OH", "cs.OS", "cs.PF", "cs.PL", "cs.RO", "cs.SC",
        "cs.SD", "cs.SE", "cs.SI", "cs.SY"
    ];
    
    /// <summary>
    /// Regex method to remove all whitespace.
    /// </summary>
    /// <returns>The regex method to remove all whitespace.</returns>
    [GeneratedRegex(@"\s+")]
    private static partial Regex CleanWhitespaceRegex();

    /// <summary>
    /// Remove markdown or LaTeX math.
    /// </summary>
    /// <returns>The cleaned string.</returns>
    [GeneratedRegex(@"\$(.*?)\$")]
    private static partial Regex RemoveMath();
    
    /// <summary>
    /// Remove markdown or LaTeX commands.
    /// </summary>
    /// <returns>The cleaned string.</returns>
    [GeneratedRegex(@"\\[a-zA-Z]+\{(.*?)\}")]
    private static partial Regex RemoveCommands();
    
    /// <summary>
    /// Remove leftover braces in markdown or LaTeX.
    /// </summary>
    /// <returns>The cleaned string.</returns>
    [GeneratedRegex("[{}]")]
    private static partial Regex RemoveExtraBraces();

    /// <summary>
    /// Regex to remove the arXiv version.
    /// </summary>
    /// <returns>The string with the version removed.</returns>
    [GeneratedRegex(@"v\d+$")]
    private static partial Regex VersionRemover();

    /// <summary>
    /// Regex to keep only numbers, replacing everything else with a space.
    /// </summary>
    /// <returns></returns>
    [GeneratedRegex(@"\D+")]
    private static partial Regex OnlyNumbers();
    
    /// <summary>
    /// Pipeline to try and automatically get rid of markdown or LaTeX.
    /// </summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    /// <summary>
    /// Save raw documents from arXiv.
    /// </summary>
    /// <param name="sortBy">The sorting method - lastUpdatedDate relevance submittedDate.</param>
    /// <param name="sortOrder">The sorting order - descending ascending.</param>
    /// <param name="maxResults">The maximum number of results to get from arXiv at once.</param>
    /// <param name="totalResults">The total number of results we want for our own database.</param>
    public static async Task SaveDocumentsGetLinksAsync(string sortBy = SortBy, string sortOrder = SortOrder, int maxResults = MaxResults, int totalResults = TotalResults)
    {
        // Ensure valid values.
        if (totalResults < 1)
        {
            totalResults = 1;
        }

        maxResults = maxResults switch
        {
            < 1 => 1,
            > TotalResults => TotalResults,
            _ => maxResults
        };

        // Ensure the directory to save raw files exists.
        string directoryPath = Values.GetDataset;
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // See how many documents already exist across all areas.
        HashSet<string> allFiles = [];
        foreach (string s in Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories))
        {
            allFiles.Add(Path.GetFileNameWithoutExtension(s));
        }

        // Get papers for all categories.
        for (int i = 0; i < Categories.Length; i++)
        {
            // Ensure the folder for this category exists.
            string categoryPath = Path.Combine(directoryPath, Categories[i]);
            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }
            
            // Get how many files are saved under this category.
            int categoryTotalResults = totalResults - Directory.GetFiles(categoryPath, "*.*", SearchOption.AllDirectories).Length;
            Console.WriteLine($"Category {i + 1} of {Categories.Length} | {Categories[i]} | {categoryTotalResults} Remaining");
            
            // Query until we have enough documents for this category.
            int startIndex = 0;
            while (categoryTotalResults > 0)
            {
                // Search for more documents.
                List<SearchDocument>? documents = await GetLinksAsync(Categories[i], sortBy, sortOrder, maxResults, startIndex, allFiles);

                // If null was returned, it means no documents were returned by the search.
                if (documents == null)
                {
                    Console.WriteLine($"Category {i + 1} of {Categories.Length} | {Categories[i]} | No results; either no more or rate limited.");
                    break;
                }
                
                // Try every document.
                foreach (SearchDocument document in documents)
                {
                    // If the file already exists in any category, there is no need to write it again.
                    if (document.ArXivId == null || document.Title == null || document.Summary == null || document.Authors == null || document.Updated == null || allFiles.Contains(document.ArXivId))
                    {
                        continue;
                    }

                    // Build the new file.
                    string contents = $"{document.Title}\n{document.Summary}\n{document.Updated.Value.Year}-{document.Updated.Value.Month}-{document.Updated.Value.Day} {document.Updated.Value.Hour}:{document.Updated.Value.Minute}:{document.Updated.Value.Second}";
                    contents = document.Authors.Aggregate(contents, (current, author) => current + $"\n{author}");

                    // Write to the new file.
                    await File.WriteAllTextAsync(Path.Combine(categoryPath, $"{document.ArXivId}.txt"), contents);
                    allFiles.Add(document.ArXivId);

                    // If we have enough documents, stop.
                    Console.WriteLine($"Category {i + 1} of {Categories.Length} | {Categories[i]} | {--categoryTotalResults} Remaining");
                    if (categoryTotalResults <= 0)
                    {
                        break;
                    }
                }

                // For the next query, index the next possible documents.
                startIndex += maxResults;
            }
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
    /// <param name="existing">Any existing results.</param>
    public static async Task<List<SearchDocument>?> GetLinksAsync(string category, string sortBy = SortBy, string sortOrder = SortOrder, int maxResults = MaxResults, int startIndex = 0, ICollection<string>? existing = null)
    {
        // Ensure valid values.
        if (maxResults < 1)
        {
            maxResults = 1;
        }

        if (startIndex < 0)
        {
            startIndex = 0;
        }

        existing ??= new SortedSet<string>();

        // Make the HTTP GET request.
        HttpResponseMessage response = await new HttpClient().GetAsync($"http://export.arxiv.org/api/query?search_query=cat:{category}&start={startIndex}&max_results={maxResults}&sortBy={sortBy}&sortOrder={sortOrder}");

        // Stop if there is an error.
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed: {response.StatusCode}");
        }

        // See how many items were returned.
        XElement[] returned = XDocument.Parse(await response.Content.ReadAsStringAsync()).Descendants($"{XmlCore}entry").ToArray();
        
        // If none were, we have either reached the end or have been rate limited.
        if (returned.Length < 1)
        {
            return null;
        }

        // Save our documents.
        List<SearchDocument> documents = [];
        foreach (XElement entry in returned)
        {
            // If any value is empty, skip this document.
            string? id = CleanElement(entry, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }
            
            // If we already have this file, skip adding it again.
            // We don't need to keep the version number.
            id = VersionRemover().Replace(id.Split('/')[^1], string.Empty);
            if (existing.Contains(id))
            {
                continue;
            }
            
            // We don't want any Markdown or LaTeX that missed the screening process.
            // This is because it is unclear how we should handle them in the UI.
            // Future implementations could try to implement this.
            string? title = CleanElement(entry, "title");
            if (string.IsNullOrWhiteSpace(title) || ContainsMarkdownOrLatex(title))
            {
                continue;
            }
            
            string? summary = CleanElement(entry, "summary");
            if (string.IsNullOrWhiteSpace(summary) || ContainsMarkdownOrLatex(summary))
            {
                continue;
            }

            DateTime? updated = GetUpdated(entry);
            if (updated == null)
            {
                continue;
            }
            
            // Add the document.
            documents.Add(new()
            {
                ArXivId = id,
                Title = title,
                Summary = summary,
                Authors = GetAuthors(entry),
                Updated = updated
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
    /// Get the time the article was last updated.
    /// </summary>
    /// <param name="entry">The XML entry.</param>
    /// <returns>The parsed time.</returns>
    private static DateTime? GetUpdated(XElement entry)
    {
        // Get the time it was last updated.
        XElement? element = entry.Element($"{XmlCore}updated");
        
        // If this is missing, look for the published time.
        if (element == null)
        {
            element = entry.Element($"{XmlCore}published");
            
            // If there is no published time, there is no date and time.
            if (element == null)
            {
                return null;
            }
        }

        // Extract all numbers from the string.
        string[] raw = OnlyNumbers().Replace(element.Value, " ").Split(' ');
        
        // Cast to numbers, padding with zeros if any happened to be missing.
        int[] values = new int[6];
        for (int i = 0; i < values.Length; i++)
        {
            try
            {
                values[i] = int.Parse(raw[i]);
            }
            catch
            {
                values[i] = 0;
            }
        }

        // Return the new date and time.
        return new DateTime(values[0], values[1], values[2], values[3], values[4], values[5]);
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
    public static string CleanString(string s)
    {
        return CleanWhitespaceRegex().Replace(ConvertToPlainText(s), " ").Trim();
    }

    /// <summary>
    /// Convert Markdown/LaTeX to plain text.
    /// </summary>
    /// <param name="s">The string to convert.</param>
    /// <returns>The converted string.</returns>
    private static string ConvertToPlainText(string s)
    {
        s = Markdown.ToPlainText(s, Pipeline);
        // Try to remove any other math expressions.
        s = RemoveMath().Replace(s, "$1");
        // Try to remove commands.
        s = RemoveCommands().Replace(s, "$1");
        // Try to remove any leftover braces.
        return RemoveExtraBraces().Replace(s, "").Trim();
    }

    /// <summary>
    /// Check if a string still contains markdown or LaTeX.
    /// </summary>
    /// <param name="s">The string to check.</param>
    /// <returns>True if there is still markdown or LaTeX, false otherwise.</returns>
    private static bool ContainsMarkdownOrLatex(string s)
    {
        // These are common markdown or LaTeX symbols, so if they still exist, we did not catch them all.
        return s.Contains('\\') || s.Contains('^') || s.Contains('_') || s.Contains('$');
    }
}