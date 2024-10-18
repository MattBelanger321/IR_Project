using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Standard;

namespace SearchEngine.Lucene.Analyzing;

/// <summary>
/// Helper analyzer for the initial loading process for building keywords and abbreviations.
/// </summary>
public class LoadingAnalyzer : Analyzer
{
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
        
        // Lowercase everything, but don't run anything else during this initial mini-loading.
        return new(tokenizer, new LowerCaseFilter(Core.Version, tokenizer));
    }
}