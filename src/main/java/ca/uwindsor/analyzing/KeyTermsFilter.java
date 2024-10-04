package ca.uwindsor.analyzing;

import ca.uwindsor.common.Constants;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.lucene.analysis.TokenFilter;
import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.util.ArrayDeque;
import java.util.Deque;
import java.util.HashSet;

/**
 * Filter for key terms.
 */
public class KeyTermsFilter extends TokenFilter
{
    /**
     * The logger used for this class.
     */
    private static final Logger logger = LogManager.getLogger(KeyTermsFilter.class);

    /**
     * The terms to match.
     */
    private static HashSet<String> terms;

    /**
     * Required value for the tokenizing.
     */
    private final CharTermAttribute termAttr = addAttribute(CharTermAttribute.class);

    /**
     * Buffer for building term pairs.
     */
    private final Deque<String> buffer = new ArrayDeque<>();

    /**
     * Create the filter.
     * @param input The previous token streams prior to this.
     */
    public KeyTermsFilter(TokenStream input)
    {
        super(input);

        // If the terms have not yet been loaded, load them.
        if (terms == null)
        {
            // Define our custom terms.
            terms = new HashSet<>();

            try
            {
                BufferedReader br = new BufferedReader(new FileReader(Constants.KEY_TERMS));
                // Read the file, with a new term on each line.
                String line;
                while ((line = br.readLine()) != null)
                {
                    String term = line.trim().toLowerCase();
                    logger.debug("Added \"" + term + "\" to terms list.");
                    terms.add(term);
                }
                br.close();
            }
            catch (IOException e)
            {
                throw new RuntimeException(e);
            }
        }

        logger.info("Constructed Key Terms Filter.");
    }

    /**
     * See if we should increment the number of tokens.
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
                    logger.debug("Reached end of stream.");
                    return false;
                }

                // Add the term to our buffer.
                buffer.addLast(termAttr.toString());
            }

            // Combine any buffered phrases.
            String currentPhrase = String.join(" ", buffer);

            // Check if this is a term we wish to match.
            if (terms.contains(currentPhrase))
            {
                logger.debug("Matched the key term \"" + currentPhrase + "\".");
                termAttr.setEmpty().append(currentPhrase);
                buffer.clear();
                return true;
            }

            // Check if there is any way this buffer could be a term.
            if (!isPotentialTerm(currentPhrase))
            {
                // This is not a match and no potential for it to be part of a larger one so clear it.
                logger.debug("Discarding the start of \"" + currentPhrase + "\" as it is not part of a key term.");
                buffer.removeFirst();
                continue;
            }

            // Load the next token if this could be a term.
            if (input.incrementToken())
            {
                buffer.addLast(termAttr.toString());
                logger.debug("Extended the key term to \"" + currentPhrase + "\".");
                continue;
            }

            // Otherwise, there are no more tokens so stop.
            logger.debug("Finished the stream.");
            buffer.clear();
            return false;
        }
    }

    /**
     * Check if there is any potential the current buffer could be a term.
     * @param currentPhrase The current phrase.
     * @return True if there is potential, false otherwise.
     */
    private boolean isPotentialTerm(String currentPhrase)
    {
        for (String phrase : terms)
        {
            if (phrase.startsWith(currentPhrase))
            {
                return true;
            }
        }

        return false;
    }

    /**
     * Reset the buffer.
     * @throws IOException Any errors from resetting.
     */
    @Override
    public void reset() throws IOException
    {
        super.reset();
        buffer.clear();
    }
}