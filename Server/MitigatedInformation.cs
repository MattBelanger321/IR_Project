using System.Text;
using Accord.Math;
using SearchEngine.Shared;

namespace SearchEngine.Server;

/// <summary>
/// Class to handle mitigated information.
/// </summary>
public static class MitigatedInformation
{
    /// <summary>
    /// The folder to save per-class mitigated information results to.
    /// </summary>
    public const string Folder = "_mitigated";
    
    /// <summary>
    /// The file to save to.
    /// </summary>
    private const string FileName = "mitigated.csv";
    
    /// <summary>
    /// Calculate cumulative mitigated information across all classes to be discarded.
    /// </summary>
    /// <param name="discard">What percentage of terms to discard.</param>
    public static async Task Perform(float discard = 0)
    {
        // Clear any existing information.
        Embeddings.DiscardTerms.Clear();
        
        // Nothing to do if the folder does not exist.
        string directory = Values.GetDataset;
        if (!Directory.Exists(directory))
        {
            return;
        }
        
        Console.WriteLine("Loading data for mitigated information...");
        
        // If there is preprocessed text, we will load it instead of running it again.
        string processedDirectory = $"{directory}{Embeddings.Processed}";

        // Count the total number of files.
        int total = 0;
        
        // Store the number of documents that are a part of each class.
        Dictionary<string, int> categories = new();
        
        // Store the frequency that terms appear in any given document.
        Dictionary<string, int> terms = new();

        // The total instances across all documents that terms appear to help with sorting.
        Dictionary<string, int> termsInstances = new();
        
        // Store the number of times that a term appears in a given category.
        Dictionary<string, Dictionary<string, int>> categoryTerms = new();
        
        // Store the cumulative mitigated information for each term across all categories.
        Dictionary<string, float> cumulativeInformation = new();

        // Load all files.
        foreach (string file in Directory.GetFiles(directory, "*.txt*", SearchOption.AllDirectories))
        {
            // Increment the number of files.
            total += 1;
            
            // Read the contents.
            string[] contents = (await File.ReadAllTextAsync(file)).Split('\n');
            
            // Store what categories this file is in.
            string[] fileCategories = contents[4].Split('|');
            foreach (string category in fileCategories)
            {
                if (categories.TryAdd(category, 1))
                {
                    categoryTerms.Add(category, []);
                }
                else
                {
                    categories[category]++;
                }
            }
            
            // Get the path for where there should be a processed file.
            string processedFile = Path.Combine(processedDirectory, $"{fileCategories[0]}.txt", Path.GetFileName(file));
            
            // If the processed file exists, load, otherwise process it now.
            string[] fileTerms = File.Exists(processedFile) ? (await File.ReadAllTextAsync(processedFile)).Split() : Embeddings.Preprocess(contents[1]).Split();
            
            // Cache the total instances of terms.
            foreach (string term in fileTerms)
            {
                if (!termsInstances.TryAdd(term, 1))
                {
                    termsInstances[term]++;
                }
            }

            fileTerms = fileTerms.Distinct();
            
            // Cache the distinct instance of every term for use in calculations.
            foreach (string term in fileTerms)
            {
                if (terms.TryAdd(term, 1))
                {
                    cumulativeInformation.Add(term, 0);
                }
                else
                {
                    terms[term]++;
                }
            }

            // Cache the per-category instance of each term.
            foreach (string category in fileCategories)
            {
                foreach (string term in fileTerms)
                {
                    if (!categoryTerms[category].TryGetValue(term, out int value))
                    {
                        categoryTerms[category].Add(term, 1);
                    }
                    else
                    {
                        categoryTerms[category][term] = value + 1;
                    }
                }
            }
        }

        // Ensure the per-category output directory exists.
        string output = $"{directory}{Folder}";
        if (!Directory.Exists(output))
        {
            Directory.CreateDirectory(output);
        }

        // Build outputs.
        StringBuilder sb = new();
        Dictionary<string, float> mitigatedInformation = new();
        
        // Calculate the mitigated information for each category.
        int count = 0;
        foreach (KeyValuePair<string, int> category in categories)
        {
            Console.WriteLine($"Calculating mitigated information for category {category.Key} | {count++} of {categories.Count}");
            
            // Calculate for every term in the category.
            foreach (KeyValuePair<string, int> term in terms)
            {
                // The number of documents which are in this category and have this term.
                float n11 = categoryTerms[category.Key].GetValueOrDefault(term.Key, 0);
                
                // The number of documents in this category but without this term.
                float n10 = category.Value - n11;
                
                // The number of documents with this term but not in this category.
                float n01 = term.Value - n11;
                
                // The number of documents not in this category and without this term.
                float n00 = total - term.Value;
                
                // Calculate the mitigated information/
                mitigatedInformation.Add(term.Key, Partial(total, n11, n01, n10)
                                                   + Partial(total, n10, n00, n11)
                                                   + Partial(total, n01, n11, n00)
                                                   + Partial(total, n00, n10, n01));

                // Add this mitigated information to the cumulative information.
                cumulativeInformation[term.Key] += mitigatedInformation[term.Key];
            }

            // Write the mitigated information for this class to a file from most important to least, breaking ties on lower term counts.
            sb.Clear();
            sb.Append("Term,Mitigated Information");
            foreach (KeyValuePair<string, float> pair in mitigatedInformation.OrderByDescending(x => x.Value).ThenBy(x => terms[x.Key]).ThenBy(x => x.Key).ThenBy(x => termsInstances[x.Key]).ToDictionary())
            {
                sb.Append($"\n{pair.Key},{pair.Value}");
            }
            
            // Save to the file.
            await File.WriteAllTextAsync(Path.Combine(output, $"{category.Key}.csv"), sb.ToString());
            
            // Reset for the next category.
            mitigatedInformation.Clear();
        }
        
        // Sort the cumulative information by most important, breaking ties on lower term counts.
        cumulativeInformation = cumulativeInformation.OrderByDescending(x => x.Value).ThenBy(x => terms[x.Key]).ThenBy(x => termsInstances[x.Key]).ToDictionary();

        // Write the cumulative mitigated information to a file.
        sb.Clear();
        sb.Append("Term,Mitigated Information");
        foreach (KeyValuePair<string, float> pair in cumulativeInformation)
        {
            sb.Append($"\n{pair.Key},{pair.Value}");
        }
        
        // Save to the file.
        await File.WriteAllTextAsync(Path.Combine(Values.GetRootDirectory() ?? string.Empty, FileName), sb.ToString());

        // If we are not discarding anything, return.
        if (discard <= 0)
        {
            return;
        }
        
        // Save what to discard.
        foreach (KeyValuePair<string, float> pair in cumulativeInformation.TakeLast((int) (cumulativeInformation.Count * discard)))
        {
            Embeddings.DiscardTerms.Add(pair.Key);
        }
    }

    /// <summary>
    /// Load cumulative mitigated information across all classes to be discarded.
    /// </summary>
    /// <param name="discard">What percentage of terms to discard.</param>
    public static async Task Load(float discard = 0)
    {
        // Clear any existing information.
        Embeddings.DiscardTerms.Clear();

        // If we are not discarding anything, return.
        if (discard <= 0)
        {
            return;
        }
        
        // Nothing to do if the file does not exist.
        string file = $"{Values.GetRootDirectory() ?? string.Empty}{FileName}";
        if (!File.Exists(file))
        {
            return;
        }

        Dictionary<string, float> loaded = new();
        
        // Load the mitigated information from the saved file.
        string[] lines = (await File.ReadAllTextAsync(file)).Split('\n');
        for (int i = 1; i < lines.Length; i++)
        {
            string[] split = lines[i].Split(',');
            loaded.Add(split[0], float.Parse(split[1]));
        }
        
        // Save what to discard.
        foreach (KeyValuePair<string, float> pair in loaded.TakeLast((int) (loaded.Count * discard)))
        {
            Embeddings.DiscardTerms.Add(pair.Key);
        }
    }

    /// <summary>
    /// Calculate a partial step of mitigated information.
    /// </summary>
    /// <param name="total">The total amount of documents.</param>
    /// <param name="target">The amount of the class and label being targeted for this partial step.</param>
    /// <param name="term">The count of items with the term but not in the class as the target.</param>
    /// <param name="label">The count of items in the class but not with the term in the target.</param>
    /// <returns>The result of this partial calculation.</returns>
    private static float Partial(float total, float target, float term, float label)
    {
        if (target <= 0 || total <= 0)
        {
            return 0;
        }

        float numerator = (target + term);
        if (numerator <= 0)
        {
            return 0;
        }

        float denominator = target + label;
        if (denominator <= 0)
        {
            return 0;
        }
        
        return target / total * MathF.Log2(total * target / (numerator * denominator));
    }
}