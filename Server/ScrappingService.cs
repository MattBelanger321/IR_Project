using SearchEngine.Shared;

namespace SearchEngine.Server;

/// <summary>
/// Service to automatically scrape arXiv.
/// </summary>
/// <param name="logger">Logger to pass.</param>
public class ScrappingService(ILogger<ScrappingService> logger) : BackgroundService
{
    /// <summary>
    /// The amount of results we want to get with the service.
    /// </summary>
    private const int Amount = 1000;

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
                await Scrape(Amount, allFiles.Count + Amount);

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
    /// <param name="maxResults">The maximum number of results to get from arXiv at once.</param>
    /// <param name="totalResults">The total number of results we want for our own database.</param>
    /// <param name="reset">If we want to reset the vector database or not.</param>
    /// <param name="similarityThreshold">How close documents must be for us to discard them.</param>
    public static async Task Scrape(int maxResults = Amount, int totalResults = Amount, bool reset = false, double similarityThreshold = SimilarityThreshold)
    {
        // Get all the documents to build our dataset.
        await ArXiv.SaveDocumentsGetLinksAsync(maxResults: maxResults, totalResults: totalResults);

        // Summarize documents.
        await Ollama.Summarize();

        // Process all documents.
        await Embeddings.Preprocess();

        // Index our files.
        await Embeddings.Index(reset, similarityThreshold);
    }
}