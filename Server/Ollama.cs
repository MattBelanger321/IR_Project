using System.Text;
using System.Text.Json;
using SearchEngine.Shared;

namespace SearchEngine.Server;

/// <summary>
/// Handle interacting with Ollama.
/// </summary>
public static class Ollama
{
    private const string Prompt = "Summarize the following article in one sentence:\n";

    /// <summary>
    /// The default Ollama endpoint.
    /// </summary>
    private const string Url = "http://localhost:11434/api/";

    /// <summary>
    /// The model to use for summarizing.
    /// </summary>
    private const string Model = "llama3.2:1b";

    /// <summary>
    /// The client.
    /// </summary>
    private static readonly HttpClient Client = new();

    /// <summary>
    /// Summarize the articles.
    /// </summary>
    /// <returns>True if all summaries were successful, false otherwise.</returns>
    public static async Task<bool> Summarize()
    {
        // Ensure the latest model is pulled.
        string? response = await GetResponse($"{Url}pull", JsonSerializer.Serialize(new
        {
            name = Model
        }));

        // If it failed to pull, do not continue.
        if (response == null)
        {
            return false;
        }

        // If there are no raw files, there is nothing to summarize.
        string rawDirectory = Values.GetDataset;
        if (!Path.Exists(rawDirectory))
        {
            return false;
        }

        // Get all files raw files.
        string[] files = Directory.GetFiles(rawDirectory, "*.*", SearchOption.AllDirectories);

        // Ensure the summaries directory exists.
        string summariesDirectory = $"{rawDirectory}{Values.Summaries}";
        if (!Path.Exists(summariesDirectory))
        {
            Directory.CreateDirectory(summariesDirectory);
        }

        // Get the names of all existing files.
        HashSet<string> existing = [];
        foreach (string s in Directory.GetFiles(summariesDirectory, "*.*", SearchOption.AllDirectories))
        {
            existing.Add(Path.GetFileNameWithoutExtension(s));
        }

        // URL for generating messages.
        const string url = $"{Url}generate";

        // Iterate over all files in our dataset.
        for (int i = 0; i < files.Length; i++)
        {
            Console.WriteLine($"Summarizing file {i + 1} of {files.Length}");

            // The ID is the file name.
            string id = Path.GetFileNameWithoutExtension(files[i]);
            string category = Path.GetFileName(Path.GetDirectoryName(files[i])) ?? "";

            // If this file already exists, skip indexing it.
            if (existing.Contains(id))
            {
                continue;
            }

            // Read the file.
            string[] file = (await File.ReadAllTextAsync(files[i])).Split("\n");

            // Request the summary.
            response = await GetResponse(url, JsonSerializer.Serialize(new
            {
                model = Model,
                prompt = $"{Prompt}Title: {file[0]}\nAbstract: {file[1]}",
                stream = false
            }));

            // If the summary failed, stop.
            if (response == null)
            {
                return false;
            }

            // Parse the JSON and get the "response" field, stopping if it fails.
            if (!JsonDocument.Parse(response).RootElement.TryGetProperty("response", out JsonElement element))
            {
                return false;
            }

            // If the element can't be parsed to a string, stop.
            response = element.GetString();
            if (response == null)
            {
                return false;
            }

            // Write the summary to its file.
            string categoryPath = Path.Combine(summariesDirectory, $"{category}");
            if (!Path.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }
            await File.WriteAllTextAsync(Path.Combine(categoryPath, $"{id}.txt"), ArXiv.CleanString(response).Replace("Here is a summary of the article in one sentence: ", string.Empty));
            existing.Add(id);
        }

        // Return true after all summaries have been completed.
        return true;
    }

    /// <summary>
    /// Request a response from Ollama.
    /// </summary>
    /// <param name="url">The URL to send the request to.</param>
    /// <param name="json">The JSON to send.</param>
    /// <returns>The response or null if it failed.</returns>
    private static async Task<string?> GetResponse(string url, string json)
    {
        try
        {
            HttpResponseMessage response = await Client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine($"Failed to get response from Ollama: {e.Message}");
            return null;
        }
    }
}