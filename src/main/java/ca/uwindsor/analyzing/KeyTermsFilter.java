package ca.uwindsor.analyzing;

import ca.uwindsor.common.TermsCollection;
import org.apache.lucene.analysis.TokenFilter;
import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

import java.io.IOException;
import java.util.ArrayDeque;
import java.util.Collection;
import java.util.Deque;

/**
 * Filter for key terms.
 */
public class KeyTermsFilter extends TokenFilter
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
     * The terms.
     */
    private final TermsCollection termsCollection;

    /**
     * Start with an existing collection.
     *
     * @param input      The token stream input.
     * @param collection The starting collection.
     */
    public KeyTermsFilter(TokenStream input, Collection<String> collection)
    {
        super(input);
        termsCollection = new TermsCollection(collection);
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
            if (termsCollection.Contains(currentPhrase, false))
            {
                termAttr.setEmpty().append(currentPhrase);
                buffer.clear();
                return true;
            }

            // Check if there is any way this buffer could be a term.
            if (termsCollection.SetStartsWith(currentPhrase, false) == null)
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