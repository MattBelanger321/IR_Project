package ca.uwindsor.analyzing;

import java.io.IOException;
import java.util.Collection;

import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

/**
 * Filter with stemming for custom terms.
 */
public class CustomStemFilter extends SetTokenFilter
{
    /**
     * Required value for the tokenizing.
     */
    private final CharTermAttribute termAttr = addAttribute(CharTermAttribute.class);

    /**
     * Do not add any terms and creates a hash set to store term.
     *
     * @param input The input stream.
     */
    public CustomStemFilter(TokenStream input)
    {
        super(input);
    }

    /**
     * Start with an existing set.
     *
     * @param input The input stream.
     * @param set   The starting set.
     */
    public CustomStemFilter(TokenStream input, Collection<String> set)
    {
        super(input, set);
    }

    /**
     * Add terms from a file which will be normalized into a hash set.
     *
     * @param input The input stream.
     * @param path  The path to the file.
     */
    public CustomStemFilter(TokenStream input, String path)
    {
        super(input, path);
    }

    /**
     * Add terms from a file into a hash set.
     *
     * @param input     The input stream.
     * @param path      The path to the file.
     * @param normalize If the terms should be normalized or not.
     */
    public CustomStemFilter(TokenStream input, String path, Boolean normalize)
    {
        super(input, path, normalize);
    }

    /**
     * Add terms from a file which will be normalized into an existing set.
     *
     * @param input The input stream.
     * @param set   The starting set.
     * @param path  The path to the file.
     */
    public CustomStemFilter(TokenStream input, Collection<String> set, String path)
    {
        super(input, set, path);
    }

    /**
     * Add terms from a file into an existing set.
     *
     * @param input     The input stream.
     * @param set       The starting set.
     * @param path      The path to the file.
     * @param normalize If the terms should be normalized or not.
     */
    public CustomStemFilter(TokenStream input, Collection<String> set, String path, Boolean normalize)
    {
        super(input, set, path, normalize);
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
        String stem = StringStartsWith(term, false);

        // If there was no stem, just keep the term the same.
        if (stem == null)
        {
            stem = term;
        }

        termAttr.setEmpty().append(stem);

        return true;
    }
}
