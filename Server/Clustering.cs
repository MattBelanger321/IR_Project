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
    /// <param name="max">The maximum clustering value to perform up to.</param>
    /// <returns>The clustering results.</returns>
    public static async Task<Dictionary<int, Dictionary<int, HashSet<string>>>> Perform(int? max = null)
    {
        // Ensure embeddings are loaded.
        Embeddings.LoadVectors();

        // Load mitigated information.
        if (Embeddings.DiscardTerms.Count < 1)
        {
            await MitigatedInformation.Load();
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

        // Insert all embeddings into an array for passing to the model.
        double[][] vectors = new double[embeddings.Count][];
        int index = 0;
        foreach (double[] vector in embeddings.Values)
        {
            vectors[index++] = vector;
        }

        // Ensure the max we go up to is the root of the files.
        int root = (int) Math.Floor(Math.Sqrt(embeddings.Count));
        if (max == null || max > root)
        {
            max = root;
        }
        
        // Ensure the directory to save the clusters exists.
        string clusteringDirectory = $"{directory}{Folder}";
        if (!Directory.Exists(clusteringDirectory))
        {
            Directory.CreateDirectory(clusteringDirectory);
        }

        // Store all results to return.
        Dictionary<int, Dictionary<int, HashSet<string>>> results = new();

        // Loop from two clusters up to the maximum amount.
        for (int n = 2; n <= max; n++)
        {
            Console.WriteLine($"Performing {n}-Means of {max}-Means clustering.");

            // Compute the clusters.
            KMeansClusterCollection clusters = new KMeans(n).Learn(vectors);

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
        foreach (string file in Directory.GetFiles(directory, "*.csv*", SearchOption.AllDirectories))
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