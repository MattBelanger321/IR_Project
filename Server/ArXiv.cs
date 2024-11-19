using System.Text.Json;
using System.Text.Json.Serialization;
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
    /// Regex to check if something is a valid arXiv ID.
    /// </summary>
    /// <returns>If this is a valid ID.</returns>
    [GeneratedRegex(@"^(?:[a-z\-]+\/\d{2}(0[1-9]|1[0-2])\d{3,4}|\d{2}(0[1-9]|1[0-2])\.\d{5})$", RegexOptions.IgnoreCase, "en-CA")]
    private static partial Regex IsValidId();
    
    /// <summary>
    /// Pipeline to try and automatically get rid of markdown or LaTeX.
    /// </summary>
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();

    /// <summary>
    /// Save raw documents from arXiv computer science categories.
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

        // See how many documents already exist across all categories.
        HashSet<string> saved = [];
        HashSet<string> paths = [];
        foreach (string s in Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories))
        {
            saved.Add(Path.GetFileNameWithoutExtension(s));
            paths.Add(s);
        }

        HashSet<string> remaining = [];

        foreach (string path in paths)
        {
            // Read the current file.
            foreach (string id in (await File.ReadAllTextAsync(path)).Split('\n')[5].Split('|'))
            {
                if (!saved.Contains(id) && IsValidId().IsMatch(id))
                {
                    remaining.Add(id);
                }
            }
        }

        Console.WriteLine($"Existing files contain {remaining.Count} references to be searched.");
        while (await GetReferences(directoryPath, saved, remaining))
        {
            Console.WriteLine($"Downloaded {saved.Count} of {totalResults} documents | {remaining.Count} new references.");
        }

        if (saved.Count >= totalResults)
        {
            Console.WriteLine($"Downloaded {saved.Count} of {totalResults} documents | Not downloading new documents.");
            return;
        }

        string[] options = new string[Categories.Length];
        for (int i = 0; i < options.Length; i++)
        {
            options[i] = $"cat: {Categories[i]}";
        }
        string combined = string.Join("+OR+", options);
        
        // Query until we have enough documents for this category.
        int startIndex = 0;
        while (saved.Count < totalResults)
        {
            // Search for more documents.
            List<SearchDocument>? documents = await GetDocumentsAsync(BuildQueryString(combined, sortBy, sortOrder, maxResults, startIndex), saved);

            // If null was returned, it means no documents were returned by the search.
            if (documents == null)
            {
                Console.WriteLine("No results; either no more or rate limited.");
                return;
            }
            
            // Try every document.
            foreach (SearchDocument document in documents)
            {
                // If the file already exists in any category, there is no need to write it again.
                if (document.ArXivId == null || document.Title == null || document.Summary == null || document.Authors == null || document.Updated == null || document.Categories == null || document.Categories.Length < 1 || document.Linked == null || saved.Contains(document.ArXivId))
                {
                    continue;
                }

                // Format the categories and authors.
                string categories = string.Join("|", document.Categories);
                string authors = string.Join("|", document.Authors);
                string links = string.Join("|", document.Linked);

                // Build the new file.
                string contents = $"{document.Title}\n{document.Summary}\n{document.Updated.Value.Year}-{document.Updated.Value.Month}-{document.Updated.Value.Day} {document.Updated.Value.Hour}:{document.Updated.Value.Minute}:{document.Updated.Value.Second}\n{authors}\n{categories}\n{links}";
                
                // Save to the primary path.
                string instancePath = Path.Combine(directoryPath, document.Categories[0]);
                if (!Directory.Exists(instancePath))
                {
                    Directory.CreateDirectory(instancePath);
                }
                
                // Write to the new file.
                await File.WriteAllTextAsync(Path.Combine(instancePath, $"{document.ArXivId}.txt"), contents);
                saved.Add(document.ArXivId);

                // If we have enough documents, stop.
                Console.WriteLine($"Downloaded {saved.Count} of {totalResults} documents | {document.ArXivId} | {document.Linked.Length} references");

                // We must get any linked documents.
                foreach (string id in document.Linked)
                {
                    remaining.Add(id);
                }
                
                while (await GetReferences(directoryPath, saved, remaining))
                {
                    Console.WriteLine($"Downloaded {saved.Count} of {totalResults} documents | {remaining.Count} new references.");
                }

                // If at any point we have enough; stop.
                if (saved.Count < totalResults)
                {
                    continue;
                }

                Console.WriteLine($"Downloaded {saved.Count} of {totalResults} documents | Stopping.");
                return;
            }

            // For the next query, index the next possible documents.
            startIndex += maxResults;
        }
    }

    /// <summary>
    /// Get citations for a paper.
    /// </summary>
    /// <param name="id">The ID of the paper.</param>
    /// <returns>The IDs of any other arXiv papers it was found this references.</returns>
    public static async Task<string[]?> QueryCitations(string id)
    {
        try
        {
            // Request references.
            HttpResponseMessage response = await new HttpClient().GetAsync($"https://opencitations.net/index/coci/api/v1/citations/10.48550/arXiv.{id}");
            
            // Parse the JSON response.
            List<OpenCitation>? references = JsonSerializer.Deserialize<List<OpenCitation>>(await response.Content.ReadAsStringAsync());
            if (references == null)
            {
                Console.WriteLine("No results getting references; potentially rate limited.");
                return null;
            }

            // Extract arXiv DOIs and convert them back to arXiv IDs.
            List<string> cites = [];
            foreach (OpenCitation reference in references)
            {
                if (!reference.Citing.Contains("10.48550/arXiv."))
                {
                    continue;
                }

                string cite = reference.Citing.Replace("10.48550/arXiv.", string.Empty);
                if (IsValidId().IsMatch(cite))
                {
                    cites.Add(cite);
                }
            }

            return cites.ToArray();
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"{id} - An error occurred getting the references - {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get all documents with references.
    /// </summary>
    /// <param name="directoryPath">The path results are in.</param>
    /// <param name="saved">The documents which have been saved already.</param>
    /// <param name="remaining">Remaining items.</param>
    /// <returns>True if anything was added, false otherwise</returns>
    private static async Task<bool> GetReferences(string directoryPath, HashSet<string> saved, HashSet<string> remaining)
    {
        int index = 0;
        HashSet<string> done = [];
        HashSet<string> found = [];
        foreach (string id in remaining)
        {
            index++;
            done.Add(id);
            
            // Search for the reference, which should only return one.
            List<SearchDocument>? documents = await GetDocumentsAsync(GetSpecificDocument(id), saved);

            // If nothing was returned, the ID does not exist.
            if (documents == null)
            {
                Console.WriteLine($"{index} of {remaining.Count} | {id} | No document found or rate limited.");
                continue;
            }
            
            // Try every document, despite knowing there should only be one.
            foreach (SearchDocument document in documents)
            {
                // If the file already exists in any category, there is no need to write it again.
                if (document.ArXivId == null || document.Title == null || document.Summary == null || document.Authors == null || document.Updated == null || document.Categories == null || document.Categories.Length < 1 || document.Linked == null || saved.Contains(document.ArXivId))
                {
                    Console.WriteLine($"{index} of {remaining.Count} | {id} | Missing information.");
                    continue;
                }

                // Format the categories and authors.
                string categories = string.Join("|", document.Categories);
                string authors = string.Join("|", document.Authors);
                string links = string.Join("|", document.Linked);

                // Build the new file.
                string contents = $"{document.Title}\n{document.Summary}\n{document.Updated.Value.Year}-{document.Updated.Value.Month}-{document.Updated.Value.Day} {document.Updated.Value.Hour}:{document.Updated.Value.Minute}:{document.Updated.Value.Second}\n{authors}\n{categories}\n{links}";
                
                // Save to the primary path.
                string instancePath = Path.Combine(directoryPath, document.Categories[0]);
                if (!Directory.Exists(instancePath))
                {
                    Directory.CreateDirectory(instancePath);
                }
                
                // Write to the new file.
                await File.WriteAllTextAsync(Path.Combine(instancePath, $"{document.ArXivId}.txt"), contents);
                saved.Add(document.ArXivId);
                found.Add(document.ArXivId);
                
                Console.WriteLine($"{index} of {remaining.Count} | {id} | Saved.");
            }
        }

        // Ensure we remove all other existing items.
        foreach (string id in done)
        {
            remaining.Remove(id);
        }

        // Update all our newly-found items.
        foreach (string id in found)
        {
            remaining.Add(id);
        }

        // If any new items were found, there is more to collect.
        return found.Count > 0;
    }

    /// <summary>
    /// Get the URL to search for a specific document.
    /// </summary>
    /// <param name="id">The ID of the document.</param>
    /// <returns>The URL to search for a specific document.</returns>
    private static string GetSpecificDocument(string id)
    {
        return $"{QueryBase}id_list={id}";
    }

    /// <summary>
    /// Get a query URL.
    /// </summary>
    /// <param name="categories">The categories to search in.</param>
    /// <param name="sortBy">The sorting method - lastUpdatedDate relevance submittedDate.</param>
    /// <param name="sortOrder">The sorting order - descending ascending.</param>
    /// <param name="maxResults">The maximum number of results to get from arXiv at once.</param>
    /// <param name="startIndex">The index to start requests from at arXiv.</param>
    /// <returns>The query URL.</returns>
    private static string BuildQueryString(string categories, string sortBy = SortBy, string sortOrder = SortOrder, int maxResults = MaxResults, int startIndex = 0)
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
        
        return $"{QueryBase}search_query={categories}&start={startIndex}&max_results={maxResults}&sortBy={sortBy}&sortOrder={sortOrder}";
    }
    
    /// <summary>
    /// Get documents from arXiv.
    /// </summary>
    /// <param name="url">The URL to search with.</param>
    /// <param name="existing">The existing files.</param>
    private static async Task<List<SearchDocument>?> GetDocumentsAsync(string url, ICollection<string>? existing = null)
    {
        existing ??= new SortedSet<string>();

        // Make the HTTP GET request.
        HttpResponseMessage response = await new HttpClient().GetAsync(url);

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
            
            string[]? links = await QueryCitations(id);
            if (links == null)
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
                Updated = updated,
                Categories = GetCategories(entry),
                Linked = links
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
        return entry.Elements($"{XmlCore}author").Select(author => CleanString(author.Element($"{XmlCore}name")?.Value ?? string.Empty)).ToArray();
    }

    /// <summary>
    /// Get the categories this is in.
    /// </summary>
    /// <param name="entry"></param>
    /// <returns></returns>
    private static string[] GetCategories(XElement entry)
    {
        // Store all categories.
        List<string> categories = [];

        // Get all categories.
        foreach (XElement element in entry.Elements($"{XmlCore}category"))
        {
            XAttribute? term = element.Attribute("term");
            if (term == null)
            {
                continue;
            }

            // If this category has not yet been added, add it.
            string category = CleanString(term.Value);
            if (!categories.Contains(category))
            {
                categories.Add(category);
            }
        }
        
        return categories.ToArray();
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
    
    /// <summary>
    /// Helper class for parsing citations.
    /// </summary>
    public class OpenCitation
    {
        /// <summary>
        /// The OCI which is not used.
        /// </summary>
        [JsonPropertyName("oci")]
        public string Oci { get; set; } = string.Empty;

        /// <summary>
        /// The paper which cited which is not used.
        /// </summary>
        [JsonPropertyName("cited")]
        public string Cited { get; set; } = string.Empty;

        /// <summary>
        /// The paper which was cited which is what we are interested in.
        /// </summary>
        [JsonPropertyName("citing")]
        public string Citing { get; set; } = string.Empty;

        /// <summary>
        /// The author which is not used.
        /// </summary>
        [JsonPropertyName("author_sc")]
        public string AuthorSc { get; set; } = string.Empty;

        /// <summary>
        /// The time this was created which is not used.
        /// </summary>
        [JsonPropertyName("creation")]
        public string Creation { get; set; } = string.Empty;

        /// <summary>
        /// The timespan which is not used.
        /// </summary>
        [JsonPropertyName("timespan")]
        public string Timespan { get; set; } = string.Empty;

        /// <summary>
        /// The journal which is not used.
        /// </summary>
        [JsonPropertyName("journal_sc")]
        public string JournalSc { get; set; } = string.Empty;
    }
}