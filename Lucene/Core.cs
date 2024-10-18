using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using SearchEngine.Lucene.Analyzing;
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
    /// Key for the URLs.
    /// </summary>
    private const string UrlKey = "url";
    
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
    /// Key for the parsed contents.
    /// </summary>
    private const string ContentsKey = "contents";
    
    /// <summary>
    /// Key for the parsed key terms.
    /// </summary>
    private const string KeyTermsKey = "keyterms";

    /// <summary>
    /// The name of the custom stems file.
    /// </summary>
    private const string StemsFile = "stems.txt";

    /// <summary>
    /// The name of the key terms file.
    /// </summary>
    private const string KeyTermsFile = "terms.txt";

    /// <summary>
    /// The default number of documents to query for.
    /// </summary>
    private const int Count = 100;
    
    /// <summary>
    /// The field for key terms to count how often they occur.
    /// </summary>
    private static readonly FieldType CountField = new()
    {
        IsIndexed = true,
        IsStored = true,
        IsTokenized = true,
        IndexOptions = IndexOptions.DOCS_AND_FREQS
    };

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
        return new StemAndKeyTermsAnalyzer(KeyTermsKey, StemsFile, KeyTermsFile);
    }
    
    public static void IndexDirectory()
    {
        string directoryPath = GetDataset;
        
        // Ensure our custom field is ready for use.
        CountField.Freeze();
        
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

        // Iterate over all files in our dataset.
        foreach (string filePath in Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories))
        {
            // Read the current file.
            string[] file = File.ReadAllText(filePath).Split("\n");

            // If this file already exists, skip indexing it.
            if (searcher != null && searcher.Search(new TermQuery(new(TitleKey, file[1])), 1).TotalHits > 0)
            {
                continue;
            }
            
            // The parsed contents consist of the title and abstract.
            string contents = $"{file[1]} {file[2]}";

            // Index the authors formatted nicely.
            string authors = file.Length > 3 ? file[3] : string.Empty;
            for (int i = 4; i < file.Length; i++)
            {
                authors += $"|{file[i]}";
            }

            // Build and add the document.
            writer.AddDocument(new Document()
            {
                new StringField(UrlKey, file[0], Field.Store.YES),
                new StringField(TitleKey, file[1], Field.Store.YES),
                new StringField(SummaryKey, file[2], Field.Store.YES),
                new StringField(AuthorsKey, authors, Field.Store.YES),
                new TextField(ContentsKey, contents, Field.Store.YES),
                new Field(KeyTermsKey, contents, CountField)
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
    /// <param name="count">The number of documents to retrieve at most.</param>
    /// <returns>The documents best matching the query.</returns>
    public static SearchDocument[] Search(string queryString, int count = Count)
    {
        // Load our index.
        using FSDirectory? indexDirectory = FSDirectory.Open(GetIndexDirectory());
        using DirectoryReader? reader = DirectoryReader.Open(indexDirectory);
        IndexSearcher searcher = new(reader);
        
        // Build the query.
        Query query;
        
        // If the query was empty, match all documents.
        if (string.IsNullOrWhiteSpace(queryString))
        {
            query = new MatchAllDocsQuery();
        }
        else
        {
            // Otherwise, search the contents and keywords.
            Analyzer analyzer = LoadAnalyzer();
            query = new BooleanQuery(false)
            {
                { new QueryParser(Version, ContentsKey, analyzer).Parse(queryString), Occur.SHOULD },
                { new QueryParser(Version, KeyTermsKey, analyzer).Parse(queryString), Occur.SHOULD }
            };
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
                Url = doc.Get(UrlKey) ?? string.Empty,
                Title = doc.Get(TitleKey) ?? string.Empty,
                Summary = doc.Get(SummaryKey) ?? string.Empty,
                Authors = authors.ToArray()
            };
        }

        return documents;
    }
}