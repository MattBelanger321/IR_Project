using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Standard;

namespace SearchEngine.Lucene.Analyzing;

/// <summary>
/// Custom analyzer pipeline performs stemming and stop word removal.
/// </summary>
public class CustomAnalyzer : Analyzer
{
    /// <summary>
    /// The custom stems.
    /// </summary>
    private readonly SortedSet<string> _stems;

    /// <summary>
    /// Build the custom analyzer.
    /// </summary>
    /// <param name="stems">The stems.</param>
    public CustomAnalyzer(SortedSet<string> stems)
    {
        _stems = stems;
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

        // Filter out stop words.
        TokenStream tokenStream = new StopFilter(Core.Version, tokenizer, StopAnalyzer.ENGLISH_STOP_WORDS_SET);
        
        // Run our custom and then porter stemming.
        tokenStream = new PorterStemFilter(new CustomStemFilter(tokenStream, _stems));
        
        // Return our pipeline.
        return new(tokenizer, tokenStream);
    }
}