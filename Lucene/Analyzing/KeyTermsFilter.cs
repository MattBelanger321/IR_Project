using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using SearchEngine.Lucene.Utility;

namespace SearchEngine.Lucene.Analyzing;

/// <summary>
/// Filter to look for only key terms.
/// </summary>
public sealed class KeyTermsFilter : TokenFilter
{
    /// <summary>
    /// Required to help with building terms.
    /// </summary>
    private readonly ICharTermAttribute _termAttr;
    
    /// <summary>
    /// Buffered terms to try and build a larger key terms.
    /// </summary>
    private readonly LinkedList<string> _buffer = new();
    
    /// <summary>
    /// The key terms.
    /// </summary>
    private readonly TermsCollection _keyTerms;
    
    /// <summary>
    /// Create the filter.
    /// </summary>
    /// <param name="input">The token stream.</param>
    /// <param name="collection">The key terms.</param>
    internal KeyTermsFilter(TokenStream input, ICollection<string> collection) : base(input)
    {
        _keyTerms = new(collection);
        _termAttr = AddAttribute<ICharTermAttribute>();
    }

    /// <summary>
    /// Look for the next token.
    /// </summary>
    /// <returns>True if a key term has been matched, false otherwise</returns>
    public override bool IncrementToken()
    {
        // Key terms can be multiple words long, so we may need to loop.
        while (true)
        {
            // If there is nothing in the buffer, we need to load more.
            if (_buffer.Count < 1)
            {
                // If there is nothing left to load, then we are done.
                if (!m_input.IncrementToken())
                {
                    return false;
                }
                
                // Add the next term to the buffer.
                _buffer.AddLast(_termAttr.ToString());
            }

            // See what our current term is.
            string currentPhrase = string.Join(" ", _buffer);

            // If this is a keyword, return true.
            if (_keyTerms.Contains(currentPhrase, false))
            {
                _termAttr.SetEmpty().Append(currentPhrase);
                _buffer.Clear();
                return true;
            }

            // Otherwise, if there is no potential that this is a keyword, remove the first portion.
            if (_keyTerms.CollectionStartsWith(currentPhrase, false) == null)
            {
                _buffer.RemoveFirst();
                continue;
            }

            // Try and add another portion to the key term.
            if (m_input.IncrementToken())
            {
                _buffer.AddLast(_termAttr.ToString());
                continue;
            }
            
            // Otherwise, there is nothing left, so clear the buffer.
            _buffer.Clear();
            return false;
        }
    }

    /// <summary>
    /// Reset to clear the buffer.
    /// </summary>
    public override void Reset()
    {
        base.Reset();
        _buffer.Clear();
    }
}