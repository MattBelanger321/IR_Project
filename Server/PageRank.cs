using System.Text;
using SearchEngine.Shared;

namespace SearchEngine.Server;

/// <summary>
/// Class to handle the page rank algorithm.
/// </summary>
public static class PageRank
{
    /// <summary>
    /// The dampening factor.
    /// </summary>
    public const double DampingFactor = 0.85;
    
    /// <summary>
    /// The tolerance to stop at.
    /// </summary>
    public const int Iterations = 100;
    
    /// <summary>
    /// If results should be sorted.
    /// </summary>
    public const double Tolerance = 1e-6;

    /// <summary>
    /// The file to save to.
    /// </summary>
    private const string FileName = "page_rank.csv";
    
    /// <summary>
    /// Perform PageRank.
    /// </summary>
    /// <param name="dampingFactor">The dampening factor.</param>
    /// <param name="iterations">The max number of iterations.</param>
    /// <param name="tolerance">The tolerance to stop at.</param>
    /// <param name="clusters">The clustering results of pages.</param>
    /// <param name="sort">If results should be sorted.</param>
    /// <returns>True results of the PageRank.</returns>
    public static async Task<Dictionary<string, double>> Perform(double dampingFactor = DampingFactor, int iterations = Iterations, double tolerance = Tolerance, Dictionary<int, Dictionary<int, HashSet<string>>>? clusters = null, bool sort = false)
    {
        // If there are no pages, there is nothing to do.
        string directoryPath = Values.GetDataset;
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }

        // Load clusters if they have not been passed.
        clusters ??= await Clustering.Load();
        
        Console.WriteLine("Loading data for PageRank...");
        
        // Store the references to all files.
        Dictionary<string, string[]> fileAuthors = [];
        Dictionary<string, string[]> fileCategories = [];
        
        // All lookup to similar files by author or category.
        Dictionary<string, HashSet<string>> authorFiles = [];
        Dictionary<string, HashSet<string>> categoryFiles = [];
        
        // Map what clusters each file is in.
        Dictionary<string, Dictionary<int, int>> fileClusters = new();
        
        // Load all files.
        foreach (string s in Directory.GetFiles(directoryPath, "*.txt", SearchOption.AllDirectories))
        {
            // Parse file information.
            string id = Path.GetFileNameWithoutExtension(s);
            string[] file = (await File.ReadAllTextAsync(s)).Split('\n');
            string[] authors = file[3] == string.Empty ? [] : file[3].Split('|');
            string[] categories = file[4] == string.Empty ? [] : file[4].Split('|');
            
            // Add the information for each file.
            fileAuthors.Add(id, authors);
            fileCategories.Add(id, categories);

            // Add the file to each corresponding author.
            foreach (string author in authors)
            {
                if (authorFiles.TryGetValue(author, out HashSet<string>? value))
                {
                    value.Add(id);
                }
                else
                {
                    authorFiles.Add(author, [id]);
                }
            }
            
            // Add the file to each corresponding category.
            foreach (string category in categories)
            {
                if (categoryFiles.TryGetValue(category, out HashSet<string>? value))
                {
                    value.Add(id);
                }
                else
                {
                    categoryFiles.Add(category, [id]);
                }
            }
            
            // Ensure there is an entry for this cluster.
            fileClusters.Add(id, new());
        }
        
        // Cache the clusters for every file.
        foreach (KeyValuePair<int, Dictionary<int, HashSet<string>>> cluster in clusters)
        {
            // Check every level of clustering.
            foreach (KeyValuePair<int, HashSet<string>> labels in cluster.Value)
            {
                // Check the label for every file.
                foreach (string label in labels.Value)
                {
                    // If this file exists, add the label it is in for this level of clustering.
                    if (fileClusters.TryGetValue(label, out Dictionary<int, int>? value))
                    {
                        value.Add(cluster.Key, labels.Key);
                    }
                }
            }
        }
        
        // Remove authors and categories which only have a single entry.
        authorFiles = authorFiles.Where(x => x.Value.Count > 1).ToDictionary();
        categoryFiles = categoryFiles.Where(x => x.Value.Count > 1).ToDictionary();

        // Remove authors which were only for said paper.
        foreach (string id in fileAuthors.Keys)
        {
            fileAuthors[id] = fileAuthors[id].Where(x => authorFiles.ContainsKey(x)).ToArray();
        }
        
        // Remove categories which were only for said paper.
        foreach (string id in fileCategories.Keys)
        {
            fileCategories[id] = fileCategories[id].Where(x => categoryFiles.ContainsKey(x)).ToArray();
        }

        // Get the total number of references each file has.
        Dictionary<string, int> files = [];
        foreach (string id in fileAuthors.Keys)
        {
            // Add the direct reference links.
            files.Add(id, 0);
            
            // Add other papers written by each author, not including this paper.
            foreach (string author in fileAuthors[id])
            {
                files[id] += authorFiles[author].Count(x => x != id);
            }

            // Add other papers in each category this paper is in, not including this paper.
            foreach (string category in fileCategories[id])
            {
                files[id] += categoryFiles[category].Count(x => x != id);
            }

            // Count how many items can be reached by each cluster.
            foreach (KeyValuePair<int, int> cluster in fileClusters[id])
            {
                files[id] += clusters[cluster.Key][cluster.Value].Count(x => x != id);
            }
        }
        
        // Store how many pages we have.
        double numPages = fileAuthors.Count;
        double initialRank = 1.0 / numPages;
        
        // We must store the ranks and then new ranks every iteration to check the level of change.
        Dictionary<string, double> ranks = [];
        Dictionary<string, double> newRanks = [];
        
        // Assign initial ranks.
        foreach (string id in fileAuthors.Keys)
        {
            ranks.Add(id, initialRank);
            newRanks.Add(id, initialRank);
        }

        // Loop for the given number of iterations.
        for (int i = 0; i < iterations; i++)
        {
            // Reset new ranks.
            foreach (string id in fileAuthors.Keys)
            {
                newRanks[id] = (1.0 - dampingFactor) / numPages;
            }
            
            // Calculate the worth of dangling values.
            double dangling = ranks.Where(kvp => files[kvp.Key] == 0).Sum(kvp => kvp.Value);
            
            // For all files which have references, we must calculate their contribution.
            foreach (KeyValuePair<string, int> kvp in files)
            {
                // Nothing to do for dangling references.
                if (kvp.Value < 1)
                {
                    continue;
                }
                
                // Determine the share value for each link.
                double share = ranks[kvp.Key] / kvp.Value;

                // Calculate for every other paper the authors have written.
                foreach (string author in fileAuthors[kvp.Key])
                {
                    foreach (string id in authorFiles[author].Where(id => id != kvp.Key))
                    {
                        newRanks[id] += dampingFactor * share;
                    }
                }
                
                // Calculate for every other paper in each category the paper is in.
                foreach (string category in fileCategories[kvp.Key])
                {
                    foreach (string id in categoryFiles[category].Where(id => id != kvp.Key))
                    {
                        newRanks[id] += dampingFactor * share;
                    }
                }

                // Calculate for every other paper in each cluster the paper is in.
                foreach (KeyValuePair<int, int> cluster in fileClusters[kvp.Key])
                {
                    foreach (string id in clusters[cluster.Key][cluster.Value].Where(x => x != kvp.Key))
                    {
                        newRanks[id] += dampingFactor * share;
                    }
                }
            }
            
            // Distribute dangling node rank equally.
            double danglingContribution = dampingFactor * dangling / numPages;
            foreach (string id in fileAuthors.Keys)
            {
                newRanks[id] += danglingContribution;
            }

            // Determine the total change in rankings.
            double sum = 0;
            foreach (string id in newRanks.Keys)
            {
                // Get the difference.
                double temp = newRanks[id] - ranks[id];
                
                // If for some reason this is NaN, do not add it to the sum.
                if (double.IsNaN(temp))
                {
                    continue;
                }
                
                // We only care about absolute change so negate if needed.
                if (temp < 0)
                {
                    temp = -temp;
                }

                // Determine the new sum, ensuring this is not NaN.
                double tempSum = temp + sum;
                if (!double.IsNaN(temp))
                {
                    sum = tempSum;
                }
            }
            
            // Check for convergence and stop if it has been reached.
            if (sum < tolerance)
            {
                Console.WriteLine($"PageRank | Iteration {i + 1} of {iterations} | {sum} < {tolerance} | Stopping Early");
                break;
            }
            
            Console.WriteLine($"PageRank | Iteration {i + 1} of {iterations} | {sum} >= {tolerance}");
            
            // Update ranks for the next iteration.
            foreach (string id in newRanks.Keys)
            {
                ranks[id] = newRanks[id];
            }
        }

        StringBuilder sb = new("ID,Score");
        foreach (KeyValuePair<string, double> rank in newRanks)
        {
            sb.Append($"\n{rank.Key},{rank.Value}");
        }
        
        await File.WriteAllTextAsync(Path.Combine(Values.GetRootDirectory() ?? string.Empty, FileName), sb.ToString());

        // Return the results, sorted by ranking and with any ties going to newer papers if sorting should be done.
        return sort ? newRanks.OrderByDescending(x => x.Value).ThenByDescending(x => x.Key).ToDictionary() : newRanks;
    }

    /// <summary>
    /// Load PageRank values.
    /// </summary>
    /// <returns>The PageRank values.</returns>
    public static async Task<Dictionary<string, double>> Load()
    {
        // Nothing to do if the folder does not exist.
        string file = $"{Values.GetDataset}{FileName}";
        if (!File.Exists(FileName))
        {
            return [];
        }
        
        // Load the ranks from the saved file.
        Dictionary<string, double> ranks = new();
        string[] lines = (await File.ReadAllTextAsync(file)).Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string[] split = lines[i].Split(',');
            ranks.Add(split[0], double.Parse(split[1]));
        }

        // Return the loaded ranks.
        return ranks;
    }
}