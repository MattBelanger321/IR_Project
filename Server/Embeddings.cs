using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Porter2Stemmer;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using SearchEngine.Shared;
using WeCantSpell.Hunspell;
using Directory = System.IO.Directory;

namespace SearchEngine.Server;

/// <summary>
/// Core embeddings properties and methods.
/// </summary>
public static class Embeddings
{
    /// <summary>
    /// Key for the IDs.
    /// </summary>
    private const string IdKey = "id";
    
    /// <summary>
    /// Key for the titles.
    /// </summary>
    private const string TitleKey = "title";
    
    /// <summary>
    /// Key for the authors.
    /// </summary>
    private const string AuthorsKey = "authors";
    
    /// <summary>
    /// Key for the summaries.
    /// </summary>
    private const string SummaryKey = "summary";
    
    /// <summary>
    /// Key for the updated time.
    /// </summary>
    private const string UpdatedKey = "updated";

    /// <summary>
    /// The preprocessed directory.
    /// </summary>
    private const string Processed = "_processed";

    /// <summary>
    /// The name of the custom stems file.
    /// </summary>
    private const string StemsFile = "stems.txt";

    /// <summary>
    /// The name of the key terms file.
    /// </summary>
    private const string KeyTermsFile = "terms.txt";

    /// <summary>
    /// The language for spell checking.
    /// </summary>
    private const string Language = "en_US";

    /// <summary>
    /// The embeddings file.
    /// </summary>
    private const string EmbeddingsFile = "embeddings.txt";

    /// <summary>
    /// The name of the vector collection.
    /// </summary>
    private const string VectorCollectionName = "arXiv";

    /// <summary>
    /// The number of times to attempt spell correction.
    /// </summary>
    private const int Attempts = 5;
    
    /// <summary>
    /// Spell checking affinity file.
    /// </summary>
    private static readonly string AffinityFile = Values.GetFilePath($"{Language}.aff");
    
    /// <summary>
    /// Spell checking dictionary file.
    /// </summary>
    private static readonly string DictionaryFile = Values.GetFilePath($"{Language}.dic");

    /// <summary>
    /// All mappings for key terms.
    /// </summary>
    private static readonly SortedSet<TermMappings> Mappings = new();

    /// <summary>
    /// All stems.
    /// </summary>
    private static readonly SortedSet<string> Stems = new();

    /// <summary>
    /// All stop words.
    /// </summary>
    private static readonly HashSet<string> StopWords = new()
    {
        "a", "an", "and", "are", "as", "at", "be",
        "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on",
        "or", "such", "that", "the", "their", "then", "there", "these", "they", "this",
        "to", "was", "will", "with"
    };

    /// <summary>
    /// The string builder for preprocessing.
    /// </summary>
    private static readonly StringBuilder Builder = new();
    
    /// <summary>
    /// The porter stemmer to run.
    /// </summary>
    private static readonly EnglishPorter2Stemmer Stemmer = new();
    
    /// <summary>
    /// The word2vec generated vectors.
    /// </summary>
    private static readonly Dictionary<string, float[]> Vectors = new();

    /// <summary>
    /// The size of the generated vectors.
    /// </summary>
    private static ulong _vectorSize;
    
    /// <summary>
    /// Qdrant connection.
    /// </summary>
    private static readonly QdrantClient VectorDatabase = new("localhost");

    /// <summary>
    /// Load all vectors.
    /// </summary>
    private static void LoadVectors()
    {
        // If already loaded, there is nothing to do.
        if (Vectors.Count > 0)
        {
            return;
        }

        // Read the embeddings file.
        string[] lines = File.ReadLines(Values.GetFilePath(EmbeddingsFile)).ToArray();
        
        // The first word of the first line is the number of vectors there are.
        _vectorSize = ulong.Parse(lines.First().Split(' ')[1]);

        // Load all word vectors.
        foreach (string line in lines.Skip(1))
        {
            // The first value is the word, with the rest being the embedding values.
            string[] tokens = line.Split(' ');
            Vectors[tokens[0]] = tokens.Skip(1).Select(float.Parse).ToArray();
        }
    }

    /// <summary>
    /// Get the embeddings for a string of text.
    /// </summary>
    /// <param name="text">The text to get the embeddings of.</param>
    /// <returns>The embeddings.</returns>
    private static float[] GetEmbeddings(string text)
    {
        return GetEmbeddings(text, out int _);
    }
    
    /// <summary>
    /// Get the embeddings for a string of text.
    /// </summary>
    /// <param name="text">The text to get the embeddings of.</param>
    /// <param name="size">The number of words which matched embeddings.</param>
    /// <returns>The embeddings.</returns>
    private static float[] GetEmbeddings(string text, out int size)
    {
        // Define the vector.
        size = 0;
        float[] vector = new float[_vectorSize];
        
        // Check every term in the text.
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            // Get the vector embedding for this word.
            float[]? wordVector = Vectors.GetValueOrDefault(word);
            
            // If there is no embedding for this word, discard it.
            if (wordVector == null)
            {
                continue;
            }

            // Add the term to the vector embeddings.
            for (ulong i = 0; i < _vectorSize; i++)
            {
                vector[i] += wordVector[i];
            }
            
            // Track that we have added a new word.
            size++;
        }

        // If there were no words, the embedding is empty.
        if (size <= 0)
        {
            return vector;
        }
        
        // Normalize the embeddings.
        for (ulong i = 0; i < _vectorSize; i++)
        {
            vector[i] /= size;
        }

        return vector;
    }
    
    /// <summary>
    /// Load a file into an existing collection.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    private static void LoadCollection(ICollection<string> collection, string path, bool normalize = true)
    {
        foreach (string s in File.ReadLines(path))
        {
            collection.Add(normalize ? s.ToLower() : s);
        }
    }

    /// <summary>
    /// LoadCollection our abbreviation mappings.
    /// </summary>
    private static void LoadMappings()
    {
        // If the abbreviations are already loaded there is nothing to do.
        if (Mappings.Count > 0)
        {
            return;
        }

        // LoadCollection the raw key terms.
        SortedSet<string> keyTerms = new();
        LoadCollection(keyTerms, Values.GetFilePath(KeyTermsFile));

        // Loop through all the key terms.
        foreach (string s in keyTerms)
        {
            // Replace whitespaces with single spaces and lowercase everything, and then split the terms.
            string[] splits = Regex.Replace(s, @"\s+", " ").ToLower().Split('|');
            
            // If there are no abbreviations, there is nothing to do.
            if (splits.Length < 2)
            {
                continue;
            }
            
            // Build all the abbreviations.
            SortedSet<string> abbreviations = new();
            for (int i = 1; i < splits.Length; i++)
            {
                // Add each abbreviation as it has been found.
                abbreviations.Add(splits[i]);

                // Get a version of the string without any special characters.
                string cleaned = Regex.Replace(splits[i], "[^a-zA-Z0-9]", string.Empty);
                
                // Add the cleaned version if it is different.
                if (splits[i] != cleaned)
                {
                    abbreviations.Add(cleaned);
                }
            }

            Mappings.Add(new(Regex.Replace(Regex.Replace(splits[0], "[^a-zA-Z0-9]+", " "), @"\s+", " ").Trim(), abbreviations));
        }
    }

    /// <summary>
    /// Preprocess a string for indexing.
    /// </summary>
    /// <param name="s">The string to preprocess.</param>
    /// <returns>The preprocessed string.</returns>
    private static string Preprocess(string s)
    {
        // Ensure mappings are loaded.
        LoadMappings();
        
        // Remove all non-alphanumeric characters (keeping parentheses), then make whitespace into single spaces and lowercase everything.
        s = Regex.Replace(Regex.Replace(s, "[^a-zA-Z0-9()]+", " "), @"\s+", " ").ToLower();
        
        // Normalize the input string to FormD (decomposed form)
        s = s.Normalize(NormalizationForm.FormD);

        // For all characters, check if the character is a non-spacing mark (diacritic).
        Builder.Clear();
        foreach (char c in from c in s let unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c) where unicodeCategory != UnicodeCategory.NonSpacingMark select c)
        {
            Builder.Append(c);
        }
        
        // Rebuild the string and then split it into words.
        s = Builder.ToString().Normalize(NormalizationForm.FormC);
        
        // Check all mappings.
        foreach (TermMappings mapping in Mappings)
        {
            // Get a regex-safe version of the main key term.
            string k = Regex.Escape(mapping.KeyTerm);

            // Replace every abbreviation.
            foreach (string a in mapping.Abbreviations.Select(Regex.Escape))
            {
                // Often, abbreviations are introduced right after a term for the first time.
                // We don't want to count this as two occurrences, so remove it.
                // Do this for both singular and plural versions.
                s = Regex.Replace(s, $@"\b{k}\s*\({a}\)", mapping.KeyTerm);
                s = Regex.Replace(s, $@"\b{k}\s*\({a}s\)", mapping.KeyTerm);
                s = Regex.Replace(s, $@"\b{k}s\s*\({a}\)", mapping.KeyTerm);
                s = Regex.Replace(s, $@"\b{k}s\s*\({a}s\)", mapping.KeyTerm);
                
                // Replace all other instances of the abbreviation with the key term.
                s = Regex.Replace(s, $@"\b{a}\b", mapping.KeyTerm);
                s = Regex.Replace(s, $@"\b{a}s\b", mapping.KeyTerm);
            }
        }
        
        // We no longer want the parentheses as the terms have been cleaned.
        string[] terms = s.Replace('(', ' ').Replace(')', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Builder.Clear();
        
        // Ensure stems are loaded.
        if (Stems.Count < 1)
        {
            LoadCollection(Stems, Values.GetFilePath(StemsFile));
        }
        
        // Remove all stop words.
        foreach (string term in terms)
        {
            // Do not add stop words.
            if (!StopWords.Contains(term))
            {
                // Run our custom stemming followed by a porter stemming.
                Builder.Append(' ').Append(Stemmer.Stem(Stems.FirstOrDefault(x => term.StartsWith(x)) ?? term).Value);
            }
        }

        // Perform one final whitespace trimming.
        s = Builder.ToString().Trim();
        Builder.Clear();
        return s;
    }

    /// <summary>
    /// Preprocess all documents.
    /// </summary>
    public static async Task Preprocess()
    {
        // Get all files.
        string directory = Values.GetDataset;
        if (!Directory.Exists(Values.GetDataset))
        {
            return;
        }
        
        string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
        
        // Get the directory that summaries could be in.
        string processedDirectory = $"{directory}{Processed}";
        if (!Directory.Exists(processedDirectory))
        {
            Directory.CreateDirectory(processedDirectory);
        }
        
        // See how many documents already exist.
        HashSet<string> allFiles = new();
        foreach (string s in Directory.GetFiles(processedDirectory, "*.*", SearchOption.AllDirectories))
        {
            allFiles.Add(Path.GetFileNameWithoutExtension(s));
        }

        // Iterate over all files in our dataset.
        for (int i = 0; i < files.Length; i++)
        {
            Console.WriteLine($"Preprocessing file {i + 1} of {files.Length}");

            // The ID is the file name.
            string id = Path.GetFileNameWithoutExtension(files[i]);

            // Check if we have already processed this file.
            if (allFiles.Contains(id))
            {
                continue;
            }
            
            // Read the current file.
            string[] file = (await File.ReadAllTextAsync(files[i])).Split("\n");

            await File.WriteAllTextAsync(Path.Combine(processedDirectory, $"{id}.txt"), Preprocess($"{file[0]} {file[1]}"));
            allFiles.Add(id);
        }
    }
    
    /// <summary>
    /// Perform indexing.
    /// </summary>
    public static async Task Index()
    {
        // Get all files.
        string directory = Values.GetDataset;
        if (!Directory.Exists(Values.GetDataset))
        {
            return;
        }
        
        // Ensure our vector mappings are loaded.
        LoadVectors();
        
        // If we cannot delete the vector embeddings, assume we just have not made them yet.
        // If it was an error, our next creating line will catch it.
        try
        {
            await VectorDatabase.DeleteCollectionAsync(VectorCollectionName);
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
            return;
        }
        
        // Create our vector database.
        await VectorDatabase.CreateCollectionAsync(VectorCollectionName, new VectorParams { Size = _vectorSize, Distance = Distance.Cosine });
        
        // Get existing summaries.
        string summariesDirectory = $"{directory}{Values.Summaries}";
        HashSet<string> summaries = new();
        if (Directory.Exists(summariesDirectory))
        {
            foreach (string file in Directory.GetFiles(summariesDirectory, "*.*", SearchOption.AllDirectories))
            {
                summaries.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        
        // Get existing preprocessed contents.
        string processedDirectory = $"{directory}{Processed}";
        HashSet<string> processed = new();
        if (Directory.Exists(processedDirectory))
        {
            foreach (string file in Directory.GetFiles(processedDirectory, "*.*", SearchOption.AllDirectories))
            {
                processed.Add(Path.GetFileNameWithoutExtension(file));
            }
        }
        
        // Iterate over all files in our dataset.
        string[] files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories);
        PointStruct[] points = new PointStruct[files.Length];
        for (int i = 0; i < files.Length; i++)
        {
            Console.WriteLine($"Indexing file {i + 1} of {files.Length}");

            // The ID is the file name.
            string id = Path.GetFileNameWithoutExtension(files[i]);
            
            // Read the current file.
            string[] file = (await File.ReadAllTextAsync(files[i])).Split("\n");

            // Index the authors formatted nicely.
            string authors = file.Length > 3 ? file[3] : string.Empty;
            for (int j = 4; j < file.Length; j++)
            {
                authors += $"|{file[j]}";
            }

            points[i] = new()
            {
                Id = (ulong) i,
                // See if we have already preprocessed the contents. Otherwise, preprocess it now.
                Vectors = GetEmbeddings(processed.Contains(id) ? await File.ReadAllTextAsync(Path.Combine(processedDirectory, $"{id}.txt")) : Preprocess($"{file[0]} {file[1]}")),
                Payload = { 
                    [IdKey] = id,
                    [TitleKey] = file[0],
                    // Try and load the LLM summary if it exists.
                    [SummaryKey] = summaries.Contains(id)? await File.ReadAllTextAsync(Path.Combine(summariesDirectory, $"{id}.txt")) : file[1],
                    [UpdatedKey] = file[2],
                    [AuthorsKey] = authors
                }
            };
        }
        
        // Update our values into the vector database.
        UpdateResult updateResult = await VectorDatabase.UpsertAsync(VectorCollectionName, points);
        if (updateResult.Status != UpdateStatus.Completed)
        {
            await Console.Error.WriteLineAsync($"Update failed: {updateResult.Status}");
        }
    }

    /// <summary>
    /// Search for documents.
    /// </summary>
    /// <param name="queryString">What we are searching for.</param>
    /// <param name="id">The ID for a similar document.</param>
    /// <param name="start">The starting search index.</param>
    /// <param name="count">The number of documents to retrieve at most.</param>
    /// <param name="attempts">The number of times to attempt spell correction.</param>
    /// <returns>The results of the query.</returns>
    public static async Task<QueryResult> Search(string? queryString = null, string? id = null, int start = 0, int count = Values.SearchCount, int attempts = Attempts)
    {
        QueryResult result = new();
        
        // Ensure our vector embeddings are loaded.
        LoadVectors();
        
        Query query;
        
        // If we are looking for similar documents, query by the ID.
        if (id != null)
        {
            query = ulong.Parse(id);
        }
        
        // Otherwise, compute based on the query string.
        else
        {
            float[] vectors = GetEmbeddings(Preprocess(queryString ?? string.Empty), out int size);
            
            // If there was no matching vectors and the query string was not empty, attempt spelling correction.
            if (size < 1 && !string.IsNullOrWhiteSpace(queryString))
            {
                if (attempts < 1)
                {
                    attempts = 1;
                }

                // Load the dictionary.
                await using FileStream dictionaryStream = File.OpenRead(DictionaryFile);
                await using FileStream affixStream = File.OpenRead(AffinityFile);
                WordList dictionary = await WordList.CreateFromStreamsAsync(dictionaryStream, affixStream);
                
                // How many times we should attempt spell checking.
                for (int i = 0; i < attempts; i++)
                {
                    // Split into every word.
                    string[] words = queryString.Split(' ');
                    for (int j = 0; j < words.Length; j++)
                    {
                        // If this already has a correct spelling, leave it.
                        if (dictionary.Check(words[j]))
                        {
                            continue;
                        }
                        
                        // Get suggestions, using the first if there is one.
                        string[] suggestions = dictionary.Suggest(words[j]).ToArray();
                        if (suggestions is { Length: > 0 })
                        {
                            // Use the first suggestion.
                            words[j] = suggestions[0];
                        }
                    }

                    // Built the corrected string.
                    string correctedQuery = Preprocess(string.Join(" ", words));
                    
                    // If the strings are equal, there is nothing else to change.
                    if (queryString == correctedQuery)
                    {
                        break;
                    }

                    // Otherwise, see if there is any improvements with the vector embeddings.
                    queryString = correctedQuery;
                    result.CorrectedQuery = queryString;
                    vectors = GetEmbeddings(Preprocess(queryString), out size);
                    
                    // If there was at least one matching vector, we are ready to query..
                    if (size > 0)
                    {
                        break;
                    }
                }
            }

            // Assign the vectors to the query.
            query = vectors;
        }
        
        if (start < 0)
        {
            start = 0;
        }

        if (count < 1)
        {
            count = 1;
        }

        // Run the query.
        IReadOnlyList<ScoredPoint> points = await VectorDatabase.QueryAsync(VectorCollectionName, query, limit: (ulong) (start + count));
        int number = Math.Min(count, points.Count - start);
        for (int i = 0; i < number; i++)
        {
            // Build the authors.
            List<string> authors = new();
            string rawAuthors = points[i].Payload[AuthorsKey].StringValue;
            if (rawAuthors != null)
            {
                authors.AddRange(rawAuthors.Split('|'));
            }
            
            // Add the document.
            result.SearchDocuments.Add(new()
            {
                IndexId = points[i].Id.Num,
                ArXivId = points[i].Payload[IdKey].StringValue,
                Title = points[i].Payload[TitleKey].StringValue,
                Summary = points[i].Payload[SummaryKey].StringValue,
                Authors = authors.ToArray(),
                Updated = DateTime.Parse(points[i].Payload[UpdatedKey].StringValue)
            });
        }

        return result;
    }
}