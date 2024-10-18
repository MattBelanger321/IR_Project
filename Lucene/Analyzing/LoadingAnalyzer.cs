using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Standard;

namespace SearchEngine.Lucene.Analyzing;

/// <summary>
/// Helper analyzer for the initial loading process for building keywords and lemmatizing.
/// </summary>
public class LoadingAnalyzer : Analyzer
{
    /// <summary>
    /// The stems for custom stemming.
    /// </summary>
    private readonly SortedSet<string> _stems;
    
    /// <summary>
    /// Load in the custom stems.
    /// </summary>
    /// <param name="stems">The custom stems.</param>
    public LoadingAnalyzer(SortedSet<string> stems)
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
        // Use the standard analyzer as we do in our main analyzer.
        Tokenizer tokenizer = new StandardTokenizer(Core.Version, reader);
        
        // Lowercase everything and then pass it to our custom stemming before the porter stemmer.
        return new(tokenizer, new PorterStemFilter(new CustomStemFilter(new LowerCaseFilter(Core.Version, tokenizer), _stems)));
    }
}