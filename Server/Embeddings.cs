﻿using System.Globalization;
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
public static partial class Embeddings
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
    /// Key for the custom scores.
    /// </summary>
    private const string ScoreKey = "score";

    /// <summary>
    /// The preprocessed directory.
    /// </summary>
    public const string Processed = "_processed";

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
    /// Vector similarity weight.
    /// </summary>
    private const double Alpha = 1;

    /// <summary>
    /// PageRank weight.
    /// </summary>
    private const double Beta = 1;

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
    private static readonly SortedSet<TermMappings> Mappings = [];

    /// <summary>
    /// All stems.
    /// </summary>
    private static readonly SortedSet<string> Stems = [];

    /// <summary>
    /// All stop words.
    /// </summary>
    private static readonly HashSet<string> StopWords =
    [
        "a", "an", "and", "are", "as", "at", "be",
        "but", "by", "for", "if", "in", "into", "is", "it", "no", "not", "of", "on",
        "or", "such", "that", "the", "their", "then", "there", "these", "they", "this",
        "to", "was", "will", "with"
    ];

    /// <summary>
    /// The string builder for preprocessing.
    /// </summary>
    private static readonly StringBuilder Builder = new();

    /// <summary>
    /// The porter stemmer to run.
    /// </summary>
    private static readonly EnglishPorter2Stemmer Stemmer = new();

    /// <summary>
    /// The mitigated information to discard.
    /// </summary>
    public static readonly HashSet<string> DiscardTerms = [];

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
    public static void LoadVectors()
    {
        // If already loaded, there is nothing to do.
        if (Vectors.Count > 0)
        {
            return;
        }

        string path = Values.GetFilePath(EmbeddingsFile);
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Embeddings file {EmbeddingsFile} does not exist.");
            return;
        }

        // Read the embeddings file.
        string[] lines = File.ReadLines(path).ToArray();

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
    public static float[] GetEmbeddings(string text)
    {
        return GetEmbeddings(text, out int _);
    }

    /// <summary>
    /// Get the embeddings for a string of text.
    /// </summary>
    /// <param name="text">The text to get the embeddings of.</param>
    /// <param name="size">The number of words which matched embeddings.</param>
    /// <returns>The embeddings.</returns>
    public static float[] GetEmbeddings(string text, out int size)
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
        SortedSet<string> keyTerms = [];
        LoadCollection(keyTerms, Values.GetFilePath(KeyTermsFile));

        // Loop through all the key terms.
        foreach (string s in keyTerms)
        {
            // Replace whitespaces with single spaces and lowercase everything, and then split the terms.
            string[] splits = RemoveWhitespace().Replace(s, " ").ToLower().Split('|');

            // If there are no abbreviations, there is nothing to do.
            if (splits.Length < 2)
            {
                continue;
            }

            // Build all the abbreviations.
            SortedSet<string> abbreviations = [];
            for (int i = 1; i < splits.Length; i++)
            {
                // Add each abbreviation as it has been found.
                abbreviations.Add(splits[i]);

                // Get a version of the string without any special characters.
                string cleaned = AlphaNumerical().Replace(splits[i], string.Empty);

                // Add the cleaned version if it is different.
                if (splits[i] != cleaned)
                {
                    abbreviations.Add(cleaned);
                }
            }

            Mappings.Add(new(RemoveWhitespace().Replace(AlphaNumericalPlus().Replace(splits[0], " "), " ").Trim(), abbreviations));
        }
    }

    /// <summary>
    /// Preprocess a string for indexing.
    /// </summary>
    /// <param name="s">The string to preprocess.</param>
    /// <param name="mitigate">If we should remove mitigated information or not.</param>
    /// <returns>The preprocessed string.</returns>
    public static string Preprocess(string s, bool mitigate = false)
    {
        // Ensure mappings are loaded.
        LoadMappings();

        // Remove all non-alphanumeric characters (keeping parentheses), then make whitespace into single spaces and lowercase everything.
        s = RemoveWhitespace().Replace(AlphaNumericalBrackets().Replace(s, " "), " ").ToLower();

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
            if (StopWords.Contains(term))
            {
                continue;
            }

            // Run our custom stemming followed by a porter stemming.
            string final = Stemmer.Stem(Stems.FirstOrDefault(x => term.StartsWith(x)) ?? term).Value;

            if (!mitigate || !DiscardTerms.Contains(final))
            {
                Builder.Append(' ').Append(final);
            }
        }

        // Perform one final whitespace trimming.
        s = Builder.ToString().Trim();
        Builder.Clear();
        return s;
    }

    /// <summary>
    /// Preprocess all documents.
    /// <param name="mitigate">If we should remove mitigated information or not.</param>
    /// </summary>
    public static async Task Preprocess(bool mitigate = false)
    {
        // Get all files.
        string directory = Values.GetDataset;
        if (!Directory.Exists(Values.GetDataset))
        {
            return;
        }

        string[] files = Directory.GetFiles(directory, "*.txt*", SearchOption.AllDirectories);

        // Get the directory that summaries could be in.
        string processedDirectory = $"{directory}{Processed}";
        if (mitigate)
        {
            processedDirectory += MitigatedInformation.Folder;
        }
        
        if (!Directory.Exists(processedDirectory))
        {
            Directory.CreateDirectory(processedDirectory);
        }

        // See how many documents already exist.
        HashSet<string> allFiles = [];
        foreach (string s in Directory.GetFiles(processedDirectory, "*.txt*", SearchOption.AllDirectories))
        {
            allFiles.Add(Path.GetFileNameWithoutExtension(s));
        }

        // Iterate over all files in our dataset.
        for (int i = 0; i < files.Length; i++)
        {
            Console.WriteLine(mitigate
                ? $"Preprocessing with mitigation file {i + 1} of {files.Length}"
                : $"Preprocessing file {i + 1} of {files.Length}");


            // The ID is the file name.
            string id = Path.GetFileNameWithoutExtension(files[i]);
            string category = Path.GetFileName(Path.GetDirectoryName(files[i])) ?? "";

            // Check if we have already processed this file.
            if (allFiles.Contains(id))
            {
                continue;
            }

            // Read the current file.
            string[] file = (await File.ReadAllTextAsync(files[i])).Split("\n");

            // Write the processed to its file.
            string categoryPath = Path.Combine(processedDirectory, $"{category}");
            if (!Path.Exists(categoryPath))
            {
                Directory.CreateDirectory(categoryPath);
            }
            
            await File.WriteAllTextAsync(Path.Combine(categoryPath, $"{id}.txt"), Preprocess($"{file[0]} {file[1]}", mitigate));
            allFiles.Add(id);
        }
    }

    /// <summary>
    /// Perform indexing.
    /// </summary>
    /// <param name="reset">If we want to reset the vector database or not.</param>
    /// <param name="similarityThreshold">How close documents must be for us to discard them.</param>
    /// <param name="ranks">PageRank values.</param>
    /// <param name="discard">What percentage of terms to discard.</param>
    public static async Task Index(bool reset = false, double? similarityThreshold = null, Dictionary<string, double>? ranks = null, float discard = 0)
    {
        // Get all files.
        string directory = Values.GetDataset;
        if (!Directory.Exists(directory))
        {
            return;
        }

        // Ensure our vector mappings are loaded.
        LoadVectors();

        // Load mitigated information.
        if (DiscardTerms.Count < 1)
        {
            await MitigatedInformation.Load(discard);
        }

        // Try and delete the vector database for a complete reset if we should.
        if (reset)
        {
            // If we cannot delete the vector embeddings, assume we just have not made them yet.
            // If it was an error, our next creating line will catch it.
            try
            {
                await VectorDatabase.DeleteCollectionAsync(VectorCollectionName);
            }
            catch
            {
                // Ignored.
            }
        }

        // Create our vector database.
        try
        {
            await VectorDatabase.CreateCollectionAsync(VectorCollectionName, new VectorParams { Size = _vectorSize, Distance = Distance.Cosine });
        }
        catch
        {
            // Ignored as it may already exist.
        }

        // Get existing summaries.
        string summariesDirectory = $"{directory}{Values.Summaries}";
        Dictionary<string, string> summaries = [];
        if (Directory.Exists(summariesDirectory))
        {
            foreach (string file in Directory.GetFiles(summariesDirectory, "*.txt*", SearchOption.AllDirectories))
            {
                summaries.Add(Path.GetFileNameWithoutExtension(file), file);
            }
        }

        // Get existing preprocessed contents.
        string processedDirectory = $"{directory}{Processed}{MitigatedInformation.Folder}";
        Dictionary<string, string> processed = [];
        if (Directory.Exists(processedDirectory))
        {
            foreach (string file in Directory.GetFiles(processedDirectory, "*.txt*", SearchOption.AllDirectories))
            {
                processed.Add(Path.GetFileNameWithoutExtension(file), file);
            }
        }

        // Iterate over all files in our dataset.
        string[] files = Directory.GetFiles(directory, "*.txt", SearchOption.AllDirectories);
        
        List<PointStruct> points = [];
        for (int i = 0; i < files.Length; i++)
        {
            Console.WriteLine($"Indexing file {i + 1} of {files.Length}");

            // The ID is the file name.
            string id = Path.GetFileNameWithoutExtension(files[i]);

            // Read the current file.
            string[] file = (await File.ReadAllTextAsync(files[i])).Split('\n');

            // See if we have already preprocessed the contents. Otherwise, preprocess it now.
            float[] embeddings = GetEmbeddings(processed.TryGetValue(id, out string? p) ? await File.ReadAllTextAsync(p) : Preprocess($"{file[0]} {file[1]}", true));

            Guid? guid = null;
            
            try
            {
                // See if there is a similar file.
                IReadOnlyList<ScoredPoint> existing = await VectorDatabase.QueryAsync(VectorCollectionName, new(embeddings), limit: 1);

                if (existing.Count > 0)
                {
                    // If it is the same file, no reason to index it again.
                    if (guid.HasValue && existing[0].Payload[IdKey].StringValue == id)
                    {
                        guid = Guid.Parse(existing[0].Id.Uuid);
                    }

                    // If the document is similar enough, skip it.
                    else if (similarityThreshold.HasValue && existing[0].Score >= similarityThreshold)
                    {
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                await Console.Error.WriteLineAsync($"Search for existing document failed: {e}");
                break;
            }

            // Create a new ID if one did not exist.
            guid ??= Guid.NewGuid();

            points.Add(new()
            {
                Id = guid,
                // See if we have already preprocessed the contents. Otherwise, preprocess it now.
                Vectors = GetEmbeddings(processed.TryGetValue(id, out string? q) ? await File.ReadAllTextAsync(q) : Preprocess($"{file[0]} {file[1]}")),
                Payload = {
                    [IdKey] = id,
                    [TitleKey] = file[0],
                    // Try and load the LLM summary if it exists.
                    [SummaryKey] = summaries.TryGetValue(id, out string? s) ? await File.ReadAllTextAsync(s) : file[1],
                    [UpdatedKey] = file[2],
                    [AuthorsKey] = file[3],
                    // Add the PageRank score.
                    [ScoreKey] = ranks == null || !ranks.TryGetValue(id, out double rank) ? 0 : rank
                }
            });
        }

        // Nothing to do if no changes.
        if (points.Count < 1)
        {
            return;
        }

        // Sort the points, as the insert order may help with default returns.
        points = points.OrderByDescending(x => x.Payload[ScoreKey].DoubleValue)
            .ThenByDescending(x => x.Payload[UpdatedKey].StringValue)
            .ThenByDescending(x => x.Payload[TitleKey].StringValue)
            .ThenByDescending(x => x.Payload[IdKey].StringValue)
            .ThenBy(x => x.Payload[TitleKey].StringValue)
            .ThenBy(x => x.Payload[AuthorsKey].StringValue)
            .ThenBy(x => x.Payload[SummaryKey].StringValue)
            .ThenBy(x => x.Id.Uuid).ToList();
        
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
    /// <param name="alpha">Vector similarity weight.</param>
    /// <param name="beta">PageRank weight.</param>
    /// <param name="discard">What percentage of terms to discard.</param>
    /// <returns>The results of the query.</returns>
    public static async Task<QueryResult> Search(string? queryString = null, string? id = null, int start = 0, int count = Values.SearchCount, int attempts = Attempts, double alpha = Alpha, double beta = Beta, float discard = 0)
    {
        QueryResult result = new();

        // Ensure our vector embeddings are loaded.
        LoadVectors();

        // Load mitigated information.
        if (DiscardTerms.Count < 1)
        {
            await MitigatedInformation.Load(discard);
        }
        
        // If we are looking for similar documents, query by the ID.
        Query query;
        if (id != null)
        {
            query = Guid.Parse(id);
        }

        // Otherwise, compute based on the query string.
        else
        {
            float[] vectors = GetEmbeddings(Preprocess(queryString ?? string.Empty, true), out int size);

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
                    string raw = string.Join(" ", words);
                    string correctedQuery = Preprocess(raw, true);

                    // If the strings are equal, there is nothing else to change.
                    if (queryString == correctedQuery)
                    {
                        break;
                    }

                    // Otherwise, see if there is any improvements with the vector embeddings.
                    queryString = correctedQuery;
                    result.CorrectedQuery = raw;
                    vectors = GetEmbeddings(Preprocess(queryString, true), out size);

                    // If there was at least one matching vector, we are ready to query.
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
        try
        {
            // Get the given batch of points.
            IReadOnlyList<ScoredPoint> points = await VectorDatabase.QueryAsync(VectorCollectionName, query, offset: (ulong) start, limit: (ulong) count);
            
            // This local batch of points is then sorted by a weighted score taking into account PageRank results.
            foreach (ScoredPoint point in points.OrderByDescending(x => ScoreResult(x, alpha, beta)).ThenByDescending(x => x.Payload[UpdatedKey].StringValue).ThenBy(x => x.Payload[TitleKey].StringValue))
            {
                // If this was the field searched for during similar searching, ignore it.
                if (id != null && id == point.Id.Uuid)
                {
                    continue;
                }
                
                // Build the authors.
                List<string> authors = [];
                string rawAuthors = point.Payload[AuthorsKey].StringValue;
                if (rawAuthors != null)
                {
                    authors.AddRange(rawAuthors.Split('|'));
                }

                // Add the document.
                result.SearchDocuments.Add(new()
                {
                    IndexId = point.Id.Uuid,
                    ArXivId = point.Payload[IdKey].StringValue,
                    Title = point.Payload[TitleKey].StringValue,
                    Summary = point.Payload[SummaryKey].StringValue,
                    Authors = authors.ToArray(),
                    Updated = DateTime.Parse(point.Payload[UpdatedKey].StringValue)
                });
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        return result;
    }

    /// <summary>
    /// Score how well a matched point is by factoring in the PageRank score.
    /// </summary>
    /// <param name="point">The point.</param>
    /// <param name="alpha">Vector similarity weight.</param>
    /// <param name="beta">PageRank weight.</param>
    /// <returns>The score.</returns>
    private static double ScoreResult(ScoredPoint point, double alpha = Alpha, double beta = Beta)
    {
        return alpha * point.Score + beta * point.Payload[ScoreKey].DoubleValue;
    }

    /// <summary>
    /// Regex to replace whitespace.
    /// </summary>
    /// <returns>The regex to remove whitespace</returns>
    [GeneratedRegex(@"\s+")]
    private static partial Regex RemoveWhitespace();

    /// <summary>
    /// Get only the alphanumerical characters.
    /// </summary>
    /// <returns>Only the alphanumerical characters.</returns>
    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex AlphaNumerical();

    /// <summary>
    /// Get only the alphanumerical characters of at least length one.
    /// </summary>
    /// <returns>Only the alphanumerical characters.</returns>
    [GeneratedRegex("[^a-zA-Z0-9]+")]
    private static partial Regex AlphaNumericalPlus();

    /// <summary>
    /// Get only the alphanumerical characters and brackets.
    /// </summary>
    /// <returns>Only the alphanumerical characters and brackets.</returns>
    [GeneratedRegex("[^a-zA-Z0-9()]+")]
    private static partial Regex AlphaNumericalBrackets();
}