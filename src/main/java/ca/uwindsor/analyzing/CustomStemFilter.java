package ca.uwindsor.analyzing;

import java.io.IOException;
import java.util.Collection;

import ca.uwindsor.common.TermsCollection;
import org.apache.lucene.analysis.TokenFilter;
import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

/**
 * Filter with stemming for custom terms.
 */
public class CustomStemFilter extends TokenFilter
{
    /**
     * Required value for the tokenizing.
     */
    private final CharTermAttribute termAttr = addAttribute(CharTermAttribute.class);

    /**
     * The terms.
     */
    private final TermsCollection termsCollection;

    /**
     * Start with an existing collection.
     *
     * @param input      The token stream input.
     * @param collection The starting collection.
     */
    public CustomStemFilter(TokenStream input, Collection<String> collection)
    {
        super(input);
        termsCollection = new TermsCollection(collection);
    }

    /**
     * See if we should increment the number of tokens.
     *
     * @return True if a stemmed term was matched, false otherwise.
     * @throws IOException Reading the input fails.
     */
    @Override
    public final boolean incrementToken() throws IOException
    {
        // First, check if there is another token to process.
        if (!input.incrementToken())
        {
            return false;
        }

        String term = termAttr.toString();

        // Check if the term is in the custom stems set
        if (term == null || term.isEmpty())
        {
            return false;
        }

        // Try to step the term.
        String stem = termsCollection.StringStartsWith(term, false);

        // If there was no stem, just keep the term the same.
        if (stem == null)
        {
            stem = term;
        }

        termAttr.setEmpty().append(stem);

        return true;
    }
}
