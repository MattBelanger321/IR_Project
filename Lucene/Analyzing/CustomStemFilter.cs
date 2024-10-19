using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using SearchEngine.Lucene.Utility;

namespace SearchEngine.Lucene.Analyzing;

/// <summary>
/// Custom stemming filter.
/// </summary>
public sealed class CustomStemFilter : TokenFilter
{
    /// <summary>
    /// Required to help with building terms.
    /// </summary>
    private readonly ICharTermAttribute _termAttr;
    
    /// <summary>
    /// The stems.
    /// </summary>
    private readonly TermsCollection _stems;
    
    /// <summary>
    /// Create the filter.
    /// </summary>
    /// <param name="input">The token stream.</param>
    /// <param name="collection">The stems.</param>
    internal CustomStemFilter(TokenStream input, ICollection<string> collection) : base(input)
    {
        _stems = new(collection);
        _termAttr = AddAttribute<ICharTermAttribute>();
    }

    /// <summary>
    /// Look for the next token.
    /// </summary>
    /// <returns>True if we still have terms, false otherwise</returns>
    public override bool IncrementToken()
    {
        // If there are no more tokens, there is nothing left to do.
        if (!m_input.IncrementToken())
        {
            return false;
        }

        // If our existing string is empty, there is nothing to do.
        string term = _termAttr.ToString();
        if (term.Length < 1)
        {
            return false;
        }

        // See if we can get a customs stem, or otherwise keep the term the same.
        _termAttr.SetEmpty().Append(_stems.StringStartsWith(term, false) ?? term);
        return true;
    }
}