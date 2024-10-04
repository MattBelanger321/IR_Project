package ca.uwindsor.analyzing;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.util.HashSet;
import java.util.Set;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.lucene.analysis.TokenFilter;
import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

/**
 * Filter with stemming for computer science terms.
 */
public class ComputerScienceStemFilter extends TokenFilter
{
    /**
     * The logger used for this class.
     */
	private static final Logger logger = LogManager.getLogger(ComputerScienceStemFilter.class);

    /**
     * Required value for the tokenizing.
     */
    private final CharTermAttribute termAttr = addAttribute(CharTermAttribute.class);

    /**
     * This set holds the stems.
     */
    private final Set<String> csStems;

    /**
     * Create the filter.
     * @param input The previous token streams prior to this.
     * @param stemsFilePath The path to load the stems from.
     */
    public ComputerScienceStemFilter(TokenStream input, String stemsFilePath)
    {
        super(input);
        this.csStems = new HashSet<>();
        initCustomStems(stemsFilePath);
        logger.info("Constructed CS Stem Filter.");
    }

    /**
     * Load the custom stems.
     * @param stemsFilePath The path to load the stems from.
     */
    private void initCustomStems(String stemsFilePath)
    {
        try (BufferedReader reader = new BufferedReader(new FileReader(stemsFilePath)))
        {
            String line;
            while ((line = reader.readLine()) != null)
            {
                String stem = line.trim();
                csStems.add(stem); // Add each stem after trimming whitespace
                logger.debug("Added \"" + stem + "\" to stems list.");
            }
        }
        catch (IOException ex)
        {
            logger.error(ex);
        }
    }

    /**
     * Returns a stemmed version of term or returns original term if there is no custom stem.
     * @param term The term to try.
     * @return A stemmed version of term or returns original term if there is no custom stem.
     */
    private String stemCSToken(String term)
    {
        for (String csStem : csStems)
        {
            // Found a string that starts with the prefix.
            if (term.startsWith(csStem))
            {
                return csStem;
            }
        }

        // To stemming so return the original term.
        return term;
    }

    /**
     * See if we should increment the number of tokens.
     * @return True if a stemmed term was matched, false otherwise.
     * @throws IOException Reading the input fails.
     */
    @Override
    public final boolean incrementToken() throws IOException
    {
        // First, check if there is another token to process.
        if (!input.incrementToken())
        {
            logger.debug("Reached end of stream.");
            return false;
        }

        String term = termAttr.toString();

        // Check if the term is in the custom stems set
        if (term == null || term.isEmpty())
        {
            logger.debug("Reached end of stream.");
            return false;
        }
      
        logger.trace("Checking term if \"" + term + "\" can be CS Stemmed");

        // Set the modified term.
        String stemmedTerm = stemCSToken(term);
        termAttr.setEmpty().append(stemmedTerm);
        
        if(!stemmedTerm.equals(term))
        {
            logger.trace("Replaced \"" + term + "\" with \"" + stemmedTerm + "\".");
        }

        return true;
    }
}
