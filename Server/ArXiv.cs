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
    /// The core XML for parsing most parts.
    /// </summary>
    private const string XmlCore = "{http://www.w3.org/2005/Atom}";

    /// <summary>
    /// The sorting methods.
    /// </summary>
    private static readonly string[] SortBy = ["lastUpdatedDate", "relevance", "submittedDate"];
    
    /// <summary>
    /// The sorting orders.
    /// </summary>
    private static readonly string[] SortOrder = ["descending", "ascending"];

    /// <summary>
    /// The upper bound for querying per-category.
    /// </summary>
    private const int PerCategory = 50000;

    /// <summary>
    /// The default maximum number of results to get from arXiv at once.
    /// </summary>
    public const int MaxResults = 2000;

    /// <summary>
    /// The default total number of results we want for our own database.
    /// </summary>
    public const int TotalResults = 2000;

    /// <summary>
    /// The delay in milliseconds.
    /// </summary>
    private const int Delay = 3000;

    /// <summary>
    /// All categories.
    /// </summary>
    private static readonly string[] Categories = [
        "cs.AI", "cs.AR", "cs.CC", "cs.CE", "cs.CG", "cs.CL", "cs.CR", "cs.CV", "cs.CY", "cs.DB", "cs.DC", "cs.DL",
        "cs.DM", "cs.DS", "cs.ET", "cs.FL", "cs.GL", "cs.GR", "cs.GT", "cs.HC", "cs.IR", "cs.IT", "cs.LG", "cs.LO",
        "cs.MA", "cs.MM", "cs.MS", "cs.NA", "cs.NE", "cs.NI", "cs.OH", "cs.OS", "cs.PF", "cs.PL", "cs.RO", "cs.SC",
        "cs.SD", "cs.SE", "cs.SI", "cs.SY", "eess.AS", "eess.IV", "eess.SP", "eess.SY", "math.AC", "math.AG", "math.AP",
        "math.AT", "math.CA", "math.CO", "math.CT", "math.CV", "math.DG", "math.DS", "math.FA", "math.GM", "math.GN",
        "math.GR", "math.GT", "math.HO", "math.IT", "math.KT", "math.LO", "math.MG", "math.MP", "math.NA", "math.NT",
        "math.OA", "math.OC", "math.PR", "math.QA", "math.RA", "math.RT", "math.SG", "math.SP", "math.ST", "math-ph",
        "stat.AP", "stat.CO", "stat.ME", "stat.ML", "stat.OT", "stat.TH", "quant-ph", "physics.acc-ph", "physics.ao-ph",
        "physics.app-ph", "physics.atm-clus", "physics.atom-ph", "physics.bio-ph", "physics.chem-ph",
        "physics.class-ph", "physics.comp-ph", "physics.data-an", "physics.ed-ph", "physics.flu-dyn", "physics.gen-ph",
        "physics.geo-ph", "physics.hist-ph", "physics.ins-det", "physics.med-ph", "physics.optics", "physics.plasm-ph",
        "physics.pop-ph", "physics.soc-ph", "physics.space-ph", "econ.EM", "econ.GN", "econ.TH",  "gr-qc", "hep-ex",
        "hep-lat", "hep-ph", "hep-th", "nucl-ex", "nucl-th", "astro-ph.CO", "astro-ph.EP", "astro-ph.GA", "astro-ph.HE",
        "astro-ph.IM", "astro-ph.SR", "cond-mat.dis-nn", "cond-mat.mes-hall", "cond-mat.mtrl-sci", "cond-mat.other",
        "cond-mat.quant-gas", "cond-mat.soft", "cond-mat.stat-mech", "cond-mat.str-el", "cond-mat.supr-con", "nlin.AO",
        "nlin.CD", "nlin.CG", "nlin.PS", "nlin.SI", "q-bio.BM", "q-bio.CB", "q-bio.GN", "q-bio.MN", "q-bio.NC",
        "q-bio.OT", "q-bio.PE", "q-bio.QM", "q-bio.SC", "q-bio.TO", "q-fin.CP", "q-fin.EC", "q-fin.GN", "q-fin.MF",
        "q-fin.PM", "q-fin.PR", "q-fin.RM", "q-fin.ST", "q-fin.TR"
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
    /// <param name="totalResults">The total number of results we want for our own database.</param>
    /// <param name="startingCategory">What category to start with.</param>
    /// <param name="startingOrder">What ordering to start with.</param>
    /// <param name="startingBy">What direction to start with.</param>
    public static async Task Scrape(int totalResults = TotalResults, string? startingCategory = null, string? startingOrder = null, string? startingBy = null)
    {
        // Ensure valid values.
        if (totalResults < 1)
        {
            totalResults = 1;
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

        // The search options.
        int startIndex = 0;
        int categoryIndex = 0;
        int sortOrderIndex = 0;
        int sortByIndex = 0;

        // See what category we should start at.
        if (startingCategory != null)
        {
            for (int i = 0; i < Categories.Length; i++)
            {
                if (startingCategory != Categories[i])
                {
                    continue;
                }

                categoryIndex = i;
                break;
            }
        }

        // See what order we should start at.
        if (startingOrder != null)
        {
            for (int i = 0; i < SortOrder.Length; i++)
            {
                if (startingOrder != SortOrder[i])
                {
                    continue;
                }

                sortOrderIndex = i;
                break;
            }
        }

        // See what direction we should start at.
        if (startingBy != null)
        {
            for (int i = 0; i < SortBy.Length; i++)
            {
                if (startingBy != SortBy[i])
                {
                    continue;
                }

                sortByIndex = i;
                break;
            }
        }
        
        // Query until we have enough documents for this category.
        while (existing.Count < totalResults)
        {
            // Make the HTTP GET request.
            string query = $"http://export.arxiv.org/api/query?search_query=cat:{Categories[categoryIndex]}&start={startIndex}&max_results={MaxResults}&sortBy={SortBy[sortByIndex]}&sortOrder={SortOrder[sortOrderIndex]}";
            HttpResponseMessage response = await new HttpClient().GetAsync(query);
            
            // Ensure we are not overloading the server.
            await Task.Delay(Delay);
            
            // Wait if there is an error.
            if (!response.IsSuccessStatusCode)
            {
                await Console.Error.WriteLineAsync($"{existing.Count} of {totalResults} | Failed: {response.StatusCode} | {query}");
                continue;
            }

            // Get the parsed document.
            XDocument doc = XDocument.Parse(await response.Content.ReadAsStringAsync());
            
            // Get how many at most documents the API could give us for this query.
            XNamespace openSearch = "http://a9.com/-/spec/opensearch/1.1/";
            XElement? possibleElement = doc.Root?.Element(openSearch + "totalResults");
            if (possibleElement == null || !int.TryParse(possibleElement.Value, out int possible))
            {
                // If the element containing the total possible results was not found, we should simply wait and try again.
                await Console.Error.WriteLineAsync($"{existing.Count} of {totalResults} | API error on the serverside | {query}\n{possibleElement}");
                continue;
            }

            // See how many items were returned.
            XElement[] returned = doc.Descendants($"{XmlCore}entry").ToArray();
            Console.WriteLine($"{existing.Count} of {totalResults} | {possible} Possible | {returned.Length} Returned | {query}");

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

                    // If this category has not been listed yet, and it is valid, save it.
                    if (!categories.Contains(category) && Categories.Any(x => category == x))
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
                if (existing.Count >= totalResults)
                {
                    return;
                }
            }

            // For the next query, index the next possible documents after those which were returned.
            startIndex += returned.Length;
            
            // If we have reached the maximum amount the API can give us for this query, change the query.
            if (startIndex < Math.Min(possible, PerCategory))
            {
                continue;
            }

            // Try going to the next category.
            startIndex = 0;
            categoryIndex++;
            if (categoryIndex < Categories.Length)
            {
                continue;
            }

            // If we have tried all categories, reset the category and try the next order.
            categoryIndex = 0;
            sortOrderIndex++;
            if (sortOrderIndex < SortOrder.Length)
            {
                continue;
            }
            
            // If we have tried all orders, reset the order and try the next criteria.
            sortOrderIndex = 0;
            sortByIndex++;
            
            // If we have exhausted all possible queries, exit.
            if (sortByIndex >= SortBy.Length)
            {
                return;
            }
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