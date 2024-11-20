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
    public static async Task Perform(int? max = null)
    {
        // Ensure embeddings are loaded.
        Embeddings.LoadVectors();
        
        // Get all files.
        string directory = Values.GetDataset;
        string processedDirectory = $"{directory}{Embeddings.Processed}";
        if (!Directory.Exists(processedDirectory))
        {
            return;
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

        // Loop from two clusters up to the maximum amount.
        for (int n = 2; n <= max; n++)
        {
            Console.WriteLine($"Performing {n}-Means of {max}-Means clustering.");

            // Compute the clusters.
            KMeansClusterCollection clusters = new KMeans(n).Learn(vectors);

            // Build the CSV output.
            StringBuilder sb = new("File,Cluster");
            foreach (KeyValuePair<string, double[]> kvp in embeddings)
            {
                sb.Append($"\n{kvp.Key},{clusters.Decide(kvp.Value)}");
            }
            
            // Save the output.
            await File.WriteAllTextAsync(Path.Combine(clusteringDirectory, $"{n}.csv"), sb.ToString());
        }
    }
}