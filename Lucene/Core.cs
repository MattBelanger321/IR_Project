using System.Globalization;
using System.Text.RegularExpressions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Queries.Mlt;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using SearchEngine.Lucene.Analyzing;
using SearchEngine.Lucene.Utility;
using SearchEngine.Shared;
using Directory = System.IO.Directory;

namespace SearchEngine.Lucene;

/// <summary>
/// Core Lucene properties and methods.
/// </summary>
public static class Core
{
    /// <summary>
    /// The version of Lucene.
    /// </summary>
    public const LuceneVersion Version = LuceneVersion.LUCENE_48;
    
    /// <summary>
    /// The name of the dataset.
    /// </summary>
    private const string Dataset = "arXiv";
    
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
    /// Key for the parsed contents.
    /// </summary>
    private const string ContentsKey = "contents";

    /// <summary>
    /// The name of the custom stems file.
    /// </summary>
    private const string StemsFile = "stems.txt";

    /// <summary>
    /// The name of the key terms file.
    /// </summary>
    private const string KeyTermsFile = "terms.txt";

    /// <summary>
    /// The default search results to return.
    /// </summary>
    private const int Count = 100;

    /// <summary>
    /// All mappings for key terms.
    /// </summary>
    private static readonly SortedSet<TermMappings> Mappings = new();

    /// <summary>
    /// All stems.
    /// </summary>
    private static readonly SortedSet<string> Stems = new();

    /// <summary>
    /// Load our abbreviation mappings.
    /// </summary>
    private static void LoadMappings()
    {
        // If the abbreviations are already loaded there is nothing to do.
        if (Mappings.Count > 0)
        {
            return;
        }

        // Load the raw key terms.
        SortedSet<string> keyTerms = new();
        TermsCollection.Load(keyTerms, GetFilePath(KeyTermsFile));

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

            Mappings.Add(new(splits[0], abbreviations));
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

        // Replace hyphens with spaces, adn then replace whitespaces with single spaces and lowercase everything.
        s = Regex.Replace(s.Replace('-', ' '), @"\s+", " ").ToLower();
        
        // Check all mappings.
        foreach (TermMappings mapping in Mappings)
        {
            // Get a sanitized version of the key term.
            string k = mapping.KeyTerm.Replace('-', ' ');

            // If the key terms are different, replace all with cleaned versions.
            if (k != mapping.KeyTerm)
            {
                // Clean for both singular and plural versions.
                string escaped = Regex.Escape(mapping.KeyTerm);
                s = Regex.Replace(s, $@"\b{escaped}\b", k);
                s = Regex.Replace(s, $@"\b{escaped}s\b", k);
            }
            
            // Get a regex-safe version of the main key term.
            k = Regex.Escape(k);

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

        return s;
    }

    /// <summary>
    /// Get the path to a file.
    /// </summary>
    /// <param name="fileName">The name of the file.</param>
    /// <returns>The path to the file.</returns>
    public static string GetFilePath(string fileName)
    {
        return Path.Combine(GetRootDirectory() ?? string.Empty, fileName);
    }
    
    /// <summary>
    /// Get the root directory of the project.
    /// </summary>
    /// <returns></returns>
    public static string? GetRootDirectory()
    {
        // Traverse upwards to find the project root, as .NET projects are nested deeper with how they run.
        return Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.Parent?.Parent?.Parent?.Parent?.FullName;
    }
    
    /// <summary>
    /// Get the dataset directory.
    /// </summary>
    public static string GetDataset => Path.Combine(GetRootDirectory() ?? string.Empty, Dataset);

    /// <summary>
    /// Get the index directory.
    /// </summary>
    /// <returns></returns>
    private static string GetIndexDirectory()
    {
        string directoryName = GetDataset + "_index";
        
        // Ensure the index directory exists.
        if (!Directory.Exists(directoryName))
        {
            Directory.CreateDirectory(directoryName);
        }

        return directoryName;
    }
    
    /// <summary>
    /// Load the analyzer.
    /// </summary>
    /// <returns>The analyzer.</returns>
    private static Analyzer LoadAnalyzer()
    {
        if (Stems.Count < 1)
        {
            TermsCollection.Load(Stems, GetFilePath(StemsFile));
        }
        
        return new CustomAnalyzer(Stems);
    }
    
    /// <summary>
    /// Perform indexing.
    /// </summary>
    public static void Index()
    {
        // Open or create the Lucene index directory
        using FSDirectory indexDirectory = FSDirectory.Open(GetIndexDirectory());

        // Initialize the analyzer and index writer configuration
        IndexWriterConfig indexConfig = new(Version, LoadAnalyzer());
        
        // Create the index writer
        using IndexWriter writer = new(indexDirectory, indexConfig);

        // Try and create an index reader so we know what has been indexed already.
        DirectoryReader? reader = null;
        IndexSearcher? searcher;
        try
        {
            reader = DirectoryReader.Open(indexDirectory);
            searcher = new(reader);
        }
        catch
        {
            searcher = null;
        }

        string[] files = Directory.GetFiles(GetDataset, "*.*", SearchOption.AllDirectories);

        // Iterate over all files in our dataset.
        for (int i = 0; i < files.Length; i++)
        {
            Console.WriteLine($"Indexing file {i + 1} of {files.Length}");

            // The ID is the file name.
            string id = Path.GetFileNameWithoutExtension(files[i]);

            // If this file already exists, skip indexing it.
            if (searcher != null && searcher.Search(new TermQuery(new(IdKey, id)), 1).TotalHits > 0)
            {
                continue;
            }
            
            // Read the current file.
            string[] file = File.ReadAllText(files[i]).Split("\n");

            // Index the authors formatted nicely.
            string authors = file.Length > 3 ? file[3] : string.Empty;
            for (int j = 4; j < file.Length; j++)
            {
                authors += $"|{file[j]}";
            }

            // Build and add the document.
            writer.AddDocument(new Document
            {
                new StringField(IdKey, id, Field.Store.YES),
                new StringField(TitleKey, file[0], Field.Store.YES),
                new StringField(SummaryKey, file[1], Field.Store.YES),
                new StringField(UpdatedKey, file[2], Field.Store.YES),
                new StringField(AuthorsKey, authors, Field.Store.YES),
                new TextField(ContentsKey, Preprocess($"{file[0]} {file[1]}"), Field.Store.YES)
            });
        }

        // Commit changes and cleanup.
        writer.Flush(triggerMerge: false, applyAllDeletes: false);
        writer.Commit();
        reader?.Dispose();
    }

    /// <summary>
    /// Search for documents.
    /// </summary>
    /// <param name="queryString">What we are searching for.</param>
    /// <param name="id">The ID for a similar document.</param>
    /// <param name="count">The number of documents to retrieve at most.</param>
    /// <returns>The documents best matching the query.</returns>
    public static SearchDocument[] Search(string? queryString = null, int? id = null, int count = Count)
    {
        // Load our index.
        using FSDirectory? indexDirectory = FSDirectory.Open(GetIndexDirectory());
        using DirectoryReader? reader = DirectoryReader.Open(indexDirectory);
        IndexSearcher searcher = new(reader);

        // If an ID was passed, look for similar documents.
        Query query;
        if (id != null)
        {
            // Default standard documents similarity.
            MoreLikeThis mlt = new(reader)
            {
                Analyzer = LoadAnalyzer(),
                MinTermFreq = 1,
                MinDocFreq = 1,
                FieldNames = new[] {ContentsKey}
            };
            query = mlt.Like(id.Value);
        }
        else
        {
            // Preprocess our input.
            queryString = Preprocess(queryString ?? string.Empty);
        
            // Build the query.
            query =
                // If the query was empty, match all documents.
                string.IsNullOrWhiteSpace(queryString) ? new MatchAllDocsQuery() :
                    // Otherwise, search the contents.
                    new QueryParser(Version, ContentsKey, LoadAnalyzer()).Parse(queryString);
        }

        // Load the information for the documents.
        TopDocs topDocs = searcher.Search(query, count < 1 ? 1 : count);
        SearchDocument[] documents = new SearchDocument[topDocs.ScoreDocs.Length];
        for (int i = 0; i < documents.Length; i++)
        {
            // Get the document.
            Document doc = searcher.Doc(topDocs.ScoreDocs[i].Doc);
            
            // Build the authors.
            List<string> authors = new();
            string rawAuthors = doc.Get(AuthorsKey);
            if (rawAuthors != null)
            {
                authors.AddRange(rawAuthors.Split('|'));
            }
            
            // Add the document.
            documents[i] = new()
            {
                IndexId = topDocs.ScoreDocs[i].Doc,
                ArXivId = doc.Get(IdKey) ?? string.Empty,
                Title = doc.Get(TitleKey) ?? string.Empty,
                Summary = doc.Get(SummaryKey) ?? string.Empty,
                Authors = authors.ToArray(),
                Updated = DateTime.Parse(doc.Get(UpdatedKey))
            };
        }

        return documents;
    }

    /// <summary>
    /// Test the analyzer on a sample string.
    /// </summary>
    /// <param name="s">The string to test on.</param>
    /// <returns>The string as a result of having been parsed by our analyzer.</returns>
    public static string RunAnalyzer(string s)
    {
        // Analyze the preprocessed text.
        using TokenStream stream = LoadAnalyzer().GetTokenStream(null, Preprocess(s));
        ICharTermAttribute termAttr = stream.AddAttribute<ICharTermAttribute>();
        stream.Reset();
        
        // Parse the sample s.
        string parsed = string.Empty;
        while (stream.IncrementToken())
        {
            parsed += $"{termAttr.ToString()} ";
        }
            
        // Cleanup and return the cleaned s.
        stream.End();
        return parsed.Trim();
    }
}