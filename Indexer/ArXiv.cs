using System.Text.RegularExpressions;
using System.Xml.Linq;
using Markdig;
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
    private const int TotalResults = 1000;

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
    /// Regex method to remove illegal characters for file names.
    /// </summary>
    /// <returns>The regex method to remove all illegal characters for file names.</returns>
    [GeneratedRegex(@"[\\/:*?""<>|]")]
    private static partial Regex CleanFileName();

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
        string directoryPath = Core.GetFilePath(Core.GetDataset);
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // See how many documents already exist across all areas.
        HashSet<string> allFiles = [];
        foreach (string s in Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories))
        {
            allFiles.Add(Path.GetFileName(s));
        }

        // Get papers for all categories.
        foreach (string category in Categories)
        {
            // Ensure the folder for this category exists.
            string categoryPath = Path.Combine(directoryPath, category);
            if (!Directory.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }
            
            // Get how many files are saved under this category.
            int categoryTotalResults = totalResults - Directory.GetFiles(categoryPath, "*.*", SearchOption.AllDirectories).Length;
            
            // Query until we have enough documents for this category.
            int startIndex = 0;
            while (categoryTotalResults > 0)
            {
                // Search for more documents.
                int queried = Math.Min(maxResults, categoryTotalResults);
                List<SearchDocument> documents = await GetLinksAsync(category, sortBy, sortOrder, queried, startIndex);

                if (documents.Count < 1)
                {
                    Console.WriteLine($"{category} - No results; either no more or rate limited.");
                    break;
                }
                
                // Try every document.
                foreach (SearchDocument document in documents)
                {
                    // If the file already exists in any category, there is no need to write it again.
                    string cleanedName = $"{CleanFileName().Replace(document.Title ?? string.Empty, string.Empty)}.txt";
                    if (allFiles.Contains(cleanedName))
                    {
                        continue;
                    }

                    // Build the new file.
                    string contents = $"{document.Url}\n{document.Title}\n{document.Summary}";
                    if (document.Authors != null)
                    {
                        contents = document.Authors.Aggregate(contents, (current, author) => current + $"\n{author}");
                    }

                    // Write to the new file.
                    await File.WriteAllTextAsync(Path.Combine(categoryPath, cleanedName), contents);
                    allFiles.Add(cleanedName);

                    // If we have enough documents, stop.
                    if (--categoryTotalResults <= 0)
                    {
                        break;
                    }
                    
                    Console.WriteLine($"{category} - {categoryTotalResults} Remaining");
                }

                // For the next query, index the next possible documents.
                startIndex += queried;
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
    public static async Task<List<SearchDocument>> GetLinksAsync(string category, string sortBy = SortBy, string sortOrder = SortOrder, int maxResults = MaxResults, int startIndex = 0)
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
        return CleanWhitespaceRegex().Replace(ConvertToPlainText(s), " ").Trim();
    }

    /// <summary>
    /// Convert Markdown/LaTeX to plain text.
    /// </summary>
    /// <param name="s">The string to convert.</param>
    /// <returns>The converted string.</returns>
    private static string ConvertToPlainText(string s)
    {
        // Try to automatically convert most.
        MarkdownPipeline pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        s = Markdown.ToPlainText(s, pipeline);
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