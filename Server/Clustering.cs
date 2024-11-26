using System.Text;
using Accord.MachineLearning;
using SearchEngine.Shared;

namespace SearchEngine.Server;

/// <summary>
/// Class to handle clustering.
/// </summary>
public static class Clustering
{
    /// <summary>
    /// The folder to save clustering results to.
    /// </summary>
    private const string Folder = "_clustering";
    
    /// <summary>
    /// Perform clustering.
    /// </summary>
    /// <param name="min">The minimum clustering value to perform up from.</param>
    /// <param name="max">The maximum clustering value to perform up to.</param>
    /// <param name="training">The percentage of data to use for training.</param>
    /// <param name="discard">What percentage of terms to discard.</param>
    /// <returns>The clustering results.</returns>
    public static async Task<Dictionary<int, Dictionary<int, HashSet<string>>>> Perform(int min = 5, int? max = 5, float training = 0.1f, float discard = 0)
    {
        // Ensure embeddings are loaded.
        Embeddings.LoadVectors();

        // Load mitigated information.
        if (Embeddings.DiscardTerms.Count < 1)
        {
            await MitigatedInformation.Load(discard);
        }
        
        // Nothing to do if the folder does not exist.
        string directory = Values.GetDataset;
        string processedDirectory = $"{directory}{Embeddings.Processed}";
        if (!Directory.Exists(processedDirectory))
        {
            return new();
        }
        
        Console.WriteLine("Preparing data for clustering...");
        
        // Get existing preprocessed contents.
        Dictionary<string, double[]> embeddings = [];
        foreach (string file in Directory.GetFiles(processedDirectory, "*.txt*", SearchOption.AllDirectories))
        {
            // Get the vector embeddings.
            float[] vector = Embeddings.GetEmbeddings(await File.ReadAllTextAsync(file));
            
            // The values need to be doubles for use with this library.
            double[] doubleVector = new double[vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                doubleVector[i] = vector[i];
            }
            
            // Store the embedding.
            embeddings.Add(Path.GetFileNameWithoutExtension(file), doubleVector);
        }

        // Can't do anything if only a single document.
        if (embeddings.Count < 2)
        {
            return [];
        }

        // Ensure a valid training size.
        if (training is <= 0 or > 1)
        {
            training = 1;
        }
        
        // Get how many samples to use for training.
        int trainingSamples = Math.Max(2, (int) (embeddings.Count * training));

        // If a max value is not passed, use the root.
        max ??= (int) Math.Floor(Math.Sqrt(embeddings.Count));
        
        // We cannot have more clusters than there are elements.
        if (max > trainingSamples)
        {
            max = trainingSamples;
        }
        
        if (min > trainingSamples)
        {
            min = trainingSamples;
        }

        // Ensure at least two clusters, otherwise this is pointless.
        if (min < 2)
        {
            min = 2;
        }

        // Get the training vectors.
        double[][] trainingSet = Random.Shared.GetItems(embeddings.Values.ToArray(), trainingSamples);
        
        // Ensure the directory to save the clusters exists.
        string clusteringDirectory = $"{directory}{Folder}";
        if (!Directory.Exists(clusteringDirectory))
        {
            Directory.CreateDirectory(clusteringDirectory);
        }

        // Store all results to return.
        Dictionary<int, Dictionary<int, HashSet<string>>> results = new();
        Dictionary<int, double> errors = new();

        // Loop from two clusters up to the maximum amount.
        for (int n = min; n <= max; n++)
        {
            Console.WriteLine($"Performing {n}-Means of {max}-Means clustering.");

            // Compute the clusters.
            KMeans kMeans = new(n);
            KMeansClusterCollection clusters = kMeans.Learn(trainingSet);
            errors.Add(n, kMeans.Error);

            // Store the results for this cluster.
            Dictionary<int, HashSet<string>> result = new();

            // Build the CSV output.
            StringBuilder sb = new("File,Cluster");
            
            // Run for every embedding.
            foreach (KeyValuePair<string, double[]> kvp in embeddings)
            {
                // Get the class it is clustered in.
                int label = clusters.Decide(kvp.Value);
                
                // Save the result.
                if (result.TryGetValue(label, out HashSet<string>? value))
                {
                    value.Add(kvp.Key);
                }
                else
                {
                    result.Add(label, [kvp.Key]);
                }
                
                // Add the result to the CSV.
                sb.Append($"\n{kvp.Key},{label}");
            }
            
            // Add the results for this clustering to the full results.
            results.Add(n, result);
            
            // Save the output.
            await File.WriteAllTextAsync(Path.Combine(clusteringDirectory, $"{n}.csv"), sb.ToString());
        }

        StringBuilder errorBuilder = new("K,Error");
        foreach (KeyValuePair<int, double> error in errors)
        {
            errorBuilder.Append($"\n{error.Key},{error.Value}");
        }
        
        await File.WriteAllTextAsync(Path.Combine(Values.GetRootDirectory() ?? string.Empty, "clustering.csv"), errorBuilder.ToString());
        
        // Return the results of the clustering.
        return results;
    }

    /// <summary>
    /// Load clustering results.
    /// </summary>
    /// <returns>The clustering results.</returns>
    public static async Task<Dictionary<int, Dictionary<int, HashSet<string>>>> Load()
    {
        // Nothing to do if the folder does not exist.
        string directory = $"{Values.GetDataset}{Folder}";
        if (!Directory.Exists(directory))
        {
            return new();
        }
        
        // Store all results to return.
        Dictionary<int, Dictionary<int, HashSet<string>>> results = new();

        // Check every existing cluster.
        foreach (string file in Directory.GetFiles(directory, "*.csv", SearchOption.AllDirectories))
        {
            // Load the contents of the CSV file.
            string[] line = (await File.ReadAllTextAsync(file)).Split('\n');
            
            // Store the results for this cluster.
            Dictionary<int, HashSet<string>> result = new();

            // Load the class label for every entry.
            for (int i = 1; i < line.Length; i++)
            {
                string[] split = line[i].Split(',');
                int label = int.Parse(split[1]);
                if (result.TryGetValue(label, out HashSet<string>? value))
                {
                    value.Add(split[0]);
                }
                else
                {
                    result.Add(label, [split[0]]);
                }
            }
            
            // Add the results for this cluster.
            results.Add(int.Parse(Path.GetFileNameWithoutExtension(file)), result);
        }
        
        // Return the results of the clustering.
        return results;
    }
}