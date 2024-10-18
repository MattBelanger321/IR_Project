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
    /// The key for when the key terms only portion should be activated.
    /// </summary>
    private readonly string _keyTermsKey;

    /// <summary>
    /// Build the custom analyzer.
    /// </summary>
    /// <param name="keyTermsKey">The field key for when the key terms should be filtered for.</param>
    /// <param name="stemsFile">The file which contains the stems.</param>
    /// <param name="keyTermsFile">The file which contains the key terms.</param>
    public StemAndKeyTermsAnalyzer(string keyTermsKey, string stemsFile, string keyTermsFile) : base(PER_FIELD_REUSE_STRATEGY)
    {
        _keyTermsKey = keyTermsKey;
        
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
        LoadingAnalyzer analyzer = new(_stems);
        
        // If the key term has any abbreviations, they are separated by the "|".
        foreach (string[] splits in initialKeywords.Select(keyword => keyword.Split('|')))
        {
            // Use our loading analyzer to stem the keyword.
            using TokenStream stream = analyzer.GetTokenStream(null, splits[0]);
            ICharTermAttribute termAttr = stream.AddAttribute<ICharTermAttribute>();
            stream.Reset();

            // Build the stemmed key term.
            string parsed = string.Empty;
            while (stream.IncrementToken())
            {
                parsed += $"{termAttr.ToString()} ";
            }
            
            // Cleanup and save the stemmed key term.
            stream.End();
            parsed = parsed.Trim();
            _keyTerms.Add(parsed);

            // Add maps for all abbreviations.
            for (int i = 1; i < splits.Length; i++)
            {
                string abbreviation = splits[i].Trim();
                builder.Add(new(abbreviation), new(parsed), false);

                // Add plural abbreviations as well.
                if (abbreviation[^1] != 's')
                {
                    builder.Add(new(abbreviation + 's'), new(parsed), false);
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
        
        // Lower case everything, convert the abbreviations, then run our custom and then porter stemming.
        TokenStream tokenStream = new PorterStemFilter(new CustomStemFilter(new SynonymFilter(new LowerCaseFilter(Core.Version, tokenizer), _abbreviations, true), _stems ?? new()));
        
        // If we are only interested in key terms, run it through the key terms filter as well.
        return new(tokenizer, fieldName == _keyTermsKey ? new KeyTermsFilter(tokenStream, _keyTerms ?? new()) : tokenStream);
    }
}