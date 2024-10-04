package ca.uwindsor.analyzing;

import java.io.IOException;
import java.io.StringReader;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.Tokenizer;
import org.apache.lucene.analysis.core.LowerCaseFilter;
import org.apache.lucene.analysis.en.PorterStemFilter;
import org.apache.lucene.analysis.standard.StandardTokenizer;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

import ca.uwindsor.common.Constants;

/**
 * Implement custom logic for tokenizing and stemming.
 */
public class ComputerScienceAnalyzer extends Analyzer
{
    /**
     * The logger used for this class.
     */
    private static final Logger logger = LogManager.getLogger(ComputerScienceAnalyzer.class);

    /**
     * Store if this analyzer should only use key terms or not.
     */
    private final Boolean keywordsOnly;

    /**
     * Set up the custom analyzer.
     * @param keyTermsOnly Whether this analyzer should only use key terms or not.
     */
    public ComputerScienceAnalyzer(Boolean keyTermsOnly)
    {
        this.keywordsOnly = keyTermsOnly;

        logger.debug("Analyzer created for " + (keyTermsOnly ? "key terms only." : " custom stemming."));
    }

    /**
     * Determine how we should tokenize a field.
     * @param fieldName The field to tokenize which we do not use.
     * @return The keywords only tokenizer if we are tracking keywords, otherwise extend the standard with keywords.
     */
    @Override
    protected TokenStreamComponents createComponents(String fieldName)
    {
        // The base is the standard tokenizer.
        Tokenizer tokenizer = new StandardTokenizer();

        // Ensure we are in lowercase.
        TokenStream tokenStream = new LowerCaseFilter(tokenizer);

        // From here, we select if this is only for the keywords or for everything with will use a custom stemmer.
        return new TokenStreamComponents(tokenizer, keywordsOnly
                // Keywords only.
                ? new KeyTermsFilter(tokenStream)
                // Use a custom stemmer for the full text, and if that does not match anything, the porter stemmer.
                : new PorterStemFilter(new ComputerScienceStemFilter(tokenStream, Constants.STEMS_FILE)));
    }

    /**
     * A function used for debugging that will return the transformed text.
     * @param fieldName The name of the field to read the text of.
     * @param text The text itself.
     * @return The analyzed text.
     * @throws IOException An error reading the file.
     */
    public String analyzeText(String fieldName, String text) throws IOException
    {
        StringBuilder analyzedText = new StringBuilder();
        
        // Tokenize the text with the same analyzer used at index time.
        try (TokenStream tokenStream = this.tokenStream(fieldName, new StringReader(text)))
        {
            CharTermAttribute charTermAttr = tokenStream.addAttribute(CharTermAttribute.class);
            tokenStream.reset();

            // Collect all tokens (transformed/analyzed version of the text).
            while (tokenStream.incrementToken())
            {
                analyzedText.append(charTermAttr.toString()).append(" ");
            }

            tokenStream.end();
        }
        
        return analyzedText.toString().trim();
    }
}