using SearchEngine.Shared;

namespace SearchEngine.Server;

/// <summary>
/// Class to handle the page rank algorithm.
/// </summary>
public static class PageRank
{
    public static async Task<Dictionary<string, double>> Perform(double dampingFactor = 0.85, int iterations = 100, double tolerance = 1e-6)
    {
        // If there are no pages, there is nothing to do.
        string directoryPath = Values.GetDataset;
        if (!Directory.Exists(directoryPath))
        {
            return [];
        }
        
        Console.WriteLine("Loading data for PageRank...");
        
        // Store the references to all files.
        Dictionary<string, string[]> fileAuthors = [];
        Dictionary<string, string[]> fileCategories = [];
        
        // All lookup to similar files by author or category.
        Dictionary<string, HashSet<string>> authorFiles = [];
        Dictionary<string, HashSet<string>> categoryFiles = [];
        
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
            Console.WriteLine($"PageRank | Iteration {i + 1} of {iterations} | {sum} < {tolerance}");
            if (sum < tolerance)
            {
                break;
            }
            
            // Update ranks for the next iteration.
            foreach (string id in newRanks.Keys)
            {
                ranks[id] = newRanks[id];
            }
        }

        // Return the results, sorted by ranking and with any ties going to newer papers.
        return ranks.OrderByDescending(x => x.Value).ThenByDescending(x => x.Key).ToDictionary();
    }
}