using SearchEngine.Shared;

namespace SearchEngine.Server;

/// <summary>
/// Service to automatically scrape arXiv.
/// </summary>
/// <param name="logger">Logger to pass.</param>
public class ScrappingService(ILogger<ScrappingService> logger) : BackgroundService
{
    /// <summary>
    /// The similarity threshold.
    /// </summary>
    private const double SimilarityThreshold = 0.95;
    
    /// <summary>
    /// Work to execute.
    /// </summary>
    /// <param name="stoppingToken"></param>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Scrapping service is starting.");

        // Loop until cancelled.
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Ensure the directory for raw files exists.
                string directoryPath = Values.GetDataset;
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }

                // See how many documents already exist across all categories.
                HashSet<string> allFiles = [];
                foreach (string s in Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories))
                {
                    allFiles.Add(Path.GetFileNameWithoutExtension(s));
                }

                // Run the scrapping for more files.
                await Scrape(allFiles.Count + ArXiv.MaxResults);

                logger.LogInformation("Scrapping finished, restarting...");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred. Restarting the scrapping.");
            }
        }

        logger.LogInformation("The scrapping service has stopped.");
    }

    /// <summary>
    /// Perform scrapping of arXiv data.
    /// </summary>
    /// <param name="totalResults">The total number of results we want for our own database.</param>
    /// <param name="discard">What percentage of terms to discard.</param>
    /// <param name="dampingFactor">The PageRank dampening factor.</param>
    /// <param name="min">The minimum clustering value to perform up from.</param>
    /// <param name="max">The maximum clustering value to perform up to.</param>
    /// <param name="iterations">The max number of PageRank iterations.</param>
    /// <param name="tolerance">The PageRank tolerance to stop at.</param>
    /// <param name="reset">If we want to reset the vector database or not.</param>
    /// <param name="similarityThreshold">How close documents must be for us to discard them.</param>
    /// <param name="mitigate">If we should run mitigation.</param>
    /// <param name="cluster">If we should run clustering.</param>
    /// <param name="rank">If we should run PageRank.</param>
    /// <param name="startingCategory">What category to start with.</param>
    /// <param name="startingOrder">What ordering to start with.</param>
    /// <param name="startingBy">What direction to start with.</param>
    public static async Task Scrape(int totalResults = ArXiv.TotalResults, float discard = 0, double dampingFactor = PageRank.DampingFactor, int min = 5, int? max = 5, int iterations = PageRank.Iterations, double tolerance = PageRank.Tolerance, bool reset = false, double similarityThreshold = SimilarityThreshold, bool mitigate = true, bool cluster = true, bool rank = true, string? startingCategory = null, string? startingOrder = null, string? startingBy = null)
    {
        // Get all the documents to build our dataset.
        await ArXiv.Scrape(totalResults, startingCategory, startingOrder, startingBy);

        // Process all documents.
        await Embeddings.Preprocess();

        // Calculate mitigated information.
        if (mitigate)
        {
            await MitigatedInformation.Perform(discard);
        }
        
        // Process all documents with mitigated information.
        await Embeddings.Preprocess(true);

        // Perform clustering.
        Dictionary<int, Dictionary<int, HashSet<string>>> clusters = cluster ? await Clustering.Perform(min, max) : await Clustering.Load();

        // Perform PageRank.
        Dictionary<string, double> ranks = rank ? await PageRank.Perform(dampingFactor, iterations, tolerance, clusters) : await PageRank.Load();

        // Summarize documents.
        await Ollama.Summarize();

        // Index our files.
        await Embeddings.Index(reset, similarityThreshold, ranks);
    }
}