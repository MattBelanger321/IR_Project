package ca.uwindsor.analyzing;

import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

import java.io.IOException;
import java.util.ArrayDeque;
import java.util.Collection;
import java.util.Deque;

/**
 * Filter for key terms.
 */
public class KeyTermsFilter extends SetTokenFilter
{
    /**
     * Required value for the tokenizing.
     */
    private final CharTermAttribute termAttr = addAttribute(CharTermAttribute.class);

    /**
     * Buffer for building term pairs.
     */
    private final Deque<String> buffer = new ArrayDeque<>();

    /**
     * Do not add any terms and creates a hash set to store term.
     *
     * @param input The input stream.
     */
    public KeyTermsFilter(TokenStream input)
    {
        super(input);
    }

    /**
     * Start with an existing set.
     *
     * @param input The input stream.
     * @param set   The starting set.
     */
    public KeyTermsFilter(TokenStream input, Collection<String> set)
    {
        super(input, set);
    }

    /**
     * Add terms from a file which will be normalized into a hash set.
     *
     * @param input The input stream.
     * @param path  The path to the file.
     */
    public KeyTermsFilter(TokenStream input, String path)
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
    public KeyTermsFilter(TokenStream input, String path, Boolean normalize)
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
    public KeyTermsFilter(TokenStream input, Collection<String> set, String path)
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
    public KeyTermsFilter(TokenStream input, Collection<String> set, String path, Boolean normalize)
    {
        super(input, set, path, normalize);
    }

    /**
     * See if we should increment the number of tokens.
     *
     * @return True if a key term was matched, false otherwise.
     * @throws IOException Reading the input fails.
     */
    @Override
    public boolean incrementToken() throws IOException
    {
        // Loop until we have a match or are at the end of the file.
        while (true)
        {
            // If we have nothing in the buffer, load the next term.
            if (buffer.isEmpty())
            {
                // If there are no more tokens to load we are done.
                if (!input.incrementToken())
                {
                    return false;
                }

                // Add the term to our buffer.
                buffer.addLast(termAttr.toString());
            }

            // Combine any buffered phrases.
            String currentPhrase = String.join(" ", buffer);

            // Check if this is a term we wish to match.
            if (Contains(currentPhrase, false))
            {
                termAttr.setEmpty().append(currentPhrase);
                buffer.clear();
                return true;
            }

            // Check if there is any way this buffer could be a term.
            if (SetStartsWith(currentPhrase, false) == null)
            {
                // This is not a match and no potential for it to be part of a larger one so clear it.
                buffer.removeFirst();
                continue;
            }

            // Load the next token if this could be a term.
            if (input.incrementToken())
            {
                buffer.addLast(termAttr.toString());
                continue;
            }

            // Otherwise, there are no more tokens so stop.
            buffer.clear();
            return false;
        }
    }

    /**
     * Reset the buffer.
     *
     * @throws IOException Any errors from resetting.
     */
    @Override
    public void reset() throws IOException
    {
        super.reset();
        buffer.clear();
    }
}