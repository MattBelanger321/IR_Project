using System.Text.RegularExpressions;
using System.Xml.Linq;
using Markdig;
using SearchEngine.Shared;

namespace SearchEngine.Server;

/// <summary>
/// Handle everything related to arXiv.
/// </summary>
public static partial class ArXiv
{
    /// <summary>
    /// The base of all queries.
    /// </summary>
    private const string QueryBase = "http://export.arxiv.org/api/query?";
    
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
    private const int MaxResults = 2000;

    /// <summary>
    /// The default total number of results we want for our own database.
    /// </summary>
    private const int TotalResults = 2000;
    
    /// <summary>
    /// All arXiv computer science categories.
    /// </summary>
    private static readonly string[] ComputerScienceCategories = [
        "cs.AI", "cs.AR", "cs.CC", "cs.CE", "cs.CG", "cs.CL", "cs.CR", "cs.CV", "cs.CY", "cs.DB", "cs.DC", "cs.DL",
        "cs.DM", "cs.DS", "cs.ET", "cs.FL", "cs.GL", "cs.GR", "cs.GT", "cs.HC", "cs.IR", "cs.IT", "cs.LG", "cs.LO",
        "cs.MA", "cs.MM", "cs.MS", "cs.NA", "cs.NE", "cs.NI", "cs.OH", "cs.OS", "cs.PF", "cs.PL", "cs.RO", "cs.SC",
        "cs.SD", "cs.SE", "cs.SI", "cs.SY"
    ];

    /// <summary>
    /// Valid starts to categories so we can discard illegal ones.
    /// </summary>
    private static readonly string[] ValidCategoryStarts = [
        "cs.", "econ.", "eess.", "math.", "astro-ph.", "cond-mat.", "gr-qc", "hep-ex", "hep-lat", "hep-ph", "hep-th",
        "math-ph", "nlin.", "nucl-ex", "nucl-th", "physics.", "quant-ph", "q-bio.", "q-fin.", "stat."
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
    /// Scrape raw documents from arXiv computer science categories.
    /// </summary>
    /// <param name="sortBy">The sorting method - lastUpdatedDate relevance submittedDate.</param>
    /// <param name="sortOrder">The sorting order - descending ascending.</param>
    /// <param name="maxResults">The maximum number of results to get from arXiv at once.</param>
    /// <param name="totalResults">The total number of results we want for our own database.</param>
    public static async Task Scrape(string sortBy = SortBy, string sortOrder = SortOrder, int maxResults = MaxResults, int totalResults = TotalResults)
    {
        // Ensure valid values.
        if (totalResults < 1)
        {
            totalResults = 1;
        }

        if (maxResults < 1)
        {
            maxResults = 1;
        }

        // Ensure the directory to save raw files exists.
        string directoryPath = Values.GetDataset;
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // See how many documents already exist across all categories.
        HashSet<string> existing = [];
        foreach (string s in Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories))
        {
            existing.Add(Path.GetFileNameWithoutExtension(s));
        }

        // Nothing to do if we already have enough.
        if (existing.Count >= totalResults)
        {
            Console.WriteLine($"Downloaded {existing.Count} of {totalResults} documents | Not downloading new documents.");
            return;
        }

        // Build the query to search in all computer science categories.
        string[] options = new string[ComputerScienceCategories.Length];
        for (int i = 0; i < options.Length; i++)
        {
            options[i] = $"cat: {ComputerScienceCategories[i]}";
        }
        string query = string.Join("+OR+", options);
        
        // Query until we have enough documents for this category.
        int startIndex = 0;
        while (existing.Count < totalResults)
        {
            // Make the HTTP GET request.
            Console.WriteLine($"Requesting {maxResults} results from arXiv...");
            HttpResponseMessage response = await new HttpClient().GetAsync($"{QueryBase}search_query={query}&start={startIndex}&max_results={maxResults}&sortBy={sortBy}&sortOrder={sortOrder}");

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
                Console.WriteLine("No results found; either done or rate limited.");
                return;
            }

            // Try every document.
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
                
                // Get the time it was last updated.
                XElement? element = entry.Element($"{XmlCore}updated");
                
                // If this is missing, look for the published time.
                if (element == null)
                {
                    element = entry.Element($"{XmlCore}published");
                
                    // If there is no published time, there is no date and time.
                    if (element == null)
                    {
                        continue;
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

                // Get the new date and time.
                DateTime? updated = new DateTime(values[0], values[1], values[2], values[3], values[4], values[5]);
                
                // Get the authors, and if there are none then skip this.
                string[] authors = entry.Elements($"{XmlCore}author").Select(author => CleanString(author.Element($"{XmlCore}name")?.Value ?? string.Empty)).Distinct().ToArray();
                if (authors.Length < 1)
                {
                    continue;
                }
                
                // Store all categories.
                List<string> categories = [];

                // Get all categories.
                foreach (XElement categoryElement in entry.Elements($"{XmlCore}category"))
                {
                    XAttribute? term = categoryElement.Attribute("term");
                    if (term == null)
                    {
                        continue;
                    }

                    // If this category has not yet been added, add it.
                    string category = CleanString(term.Value);

                    // If there are illegal characters, remove this category.
                    if (category.Contains(',') || category.Contains(';') || category.Contains(' '))
                    {
                        continue;
                    }

                    // If this category has not been listed yet and it is valid, save it.
                    if (!categories.Contains(category) && ValidCategoryStarts.Any(start => category.StartsWith(start)))
                    {
                        categories.Add(category);
                    }
                }
                
                // If there are no categories, skip this.
                if (categories.Count < 1)
                {
                    continue;
                }
                
                // Format fields to store.
                string categoriesString = string.Join("|", categories);
                string authorsString = string.Join("|", authors);

                // Build the new file.
                string contents = $"{title}\n{summary}\n{updated.Value.Year}-{updated.Value.Month}-{updated.Value.Day} {updated.Value.Hour}:{updated.Value.Minute}:{updated.Value.Second}\n{authorsString}\n{categoriesString}";
                
                // Save to the main category.
                string instancePath = Path.Combine(directoryPath, categories[0]);
                if (!Directory.Exists(instancePath))
                {
                    Directory.CreateDirectory(instancePath);
                }
                
                // Write to the new file.
                await File.WriteAllTextAsync(Path.Combine(instancePath, $"{id}.txt"), contents);
                existing.Add(id);

                // If we have enough documents, stop.
                if (existing.Count < totalResults)
                {
                    Console.WriteLine($"Downloaded {existing.Count} of {totalResults} documents | {id}");
                    continue;
                }

                Console.WriteLine($"Downloaded {existing.Count} of {totalResults} documents | {id} | Stopping.");
                return;
            }

            // For the next query, index the next possible documents.
            startIndex += maxResults;
        }
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
        // Convert LaTeX quotes to regular ones.
        s = s.Replace("``", "\"");
        s = s.Replace("''", "\"");
        // Convert LaTeX "--" into just "-".
        s = s.Replace("--", "-");
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