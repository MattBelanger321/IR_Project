using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Synonym;
using Lucene.Net.Analysis.TokenAttributes;
using SearchEngine.Lucene.Utility;

namespace SearchEngine.Lucene.Analyzing;

/// <summary>
/// Custom analyzer pipeline that performs both custom stemming and key terms.
/// </summary>
public class StemAndKeyTermsAnalyzer : Analyzer
{
    /// <summary>
    /// The custom stems.
    /// </summary>
    private static SortedSet<string>? _stems;

    /// <summary>
    /// The key terms.
    /// </summary>
    private static SortedSet<string>? _keyTerms;

    /// <summary>
    /// The abbreviations for key terms which should be considered equivalent.
    /// </summary>
    private static SynonymMap? _abbreviations;

    /// <summary>
    /// Build the custom analyzer.
    /// </summary>
    /// <param name="stemsFile">The file which contains the stems.</param>
    /// <param name="keyTermsFile">The file which contains the key terms.</param>
    public StemAndKeyTermsAnalyzer(string stemsFile, string keyTermsFile)
    {
        // If everything is already loaded, there is nothing to do.
        if (_stems != null && _keyTerms != null && _abbreviations != null)
        {
            return;
        }

        // Load the stems.
        _stems = new();
        TermsCollection.Load(_stems, Core.GetFilePath(stemsFile));
        
        // Load temporary keywords as we need to parse these.
        SortedSet<string> initialKeywords = new();
        TermsCollection.Load(initialKeywords, Core.GetFilePath(keyTermsFile));

        // Build our real key terms.
        _keyTerms = new();
        SynonymMap.Builder builder = new(true);
        LoadingAnalyzer analyzer = new();
        
        // If the key term has any abbreviations, they are separated by the "|".
        foreach (string[] splits in initialKeywords.Select(keyword => keyword.Split('|')))
        {
            // Run every key term and abbreviation through the base analyzing.
            for (int i = 0; i < splits.Length; i++)
            {
                splits[i] = Core.RunAnalyzer(splits[i], analyzer);
            }
            
            // Save the parsed key term.
            _keyTerms.Add(splits[0]);

            // Add maps for all abbreviations.
            for (int i = 1; i < splits.Length; i++)
            {
                builder.Add(new(splits[i]), new(splits[0]), false);

                // Add plural abbreviations as well.
                if (splits[i][^1] != 's')
                {
                    builder.Add(new(splits[i] + 's'), new(splits[0]), false);
                }
            }
        }
        
        // Build the final abbreviations.
        _abbreviations = builder.Build();
    }
    
    /// <summary>
    /// Build the tokenizing process.
    /// </summary>
    /// <param name="fieldName">The name of a field.</param>
    /// <param name="reader">The reader.</param>
    /// <returns>The tokenizing process.</returns>
    protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
    {
        // Use the standard analyzer.
        Tokenizer tokenizer = new StandardTokenizer(Core.Version, reader);
        
        // Convert everything to lowercase.
        TokenStream tokenStream = new LowerCaseFilter(Core.Version, tokenizer);

        // Replace all abbreviations with their respective keywords.
        tokenStream = new SynonymFilter(tokenStream, _abbreviations, true);

        // Filter out stop words.
        tokenStream = new StopFilter(Core.Version, tokenStream, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
        
        // Run our custom and then porter stemming.
        tokenStream = new PorterStemFilter(new CustomStemFilter(tokenStream, _stems ?? new()));

        // Return our pipeline.
        return new(tokenizer, tokenStream);
    }
}