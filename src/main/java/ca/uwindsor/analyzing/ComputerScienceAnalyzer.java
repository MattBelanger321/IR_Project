package ca.uwindsor.analyzing;

import java.io.IOException;
import java.io.StringReader;
import java.util.TreeSet;

import ca.uwindsor.common.TermsCollection;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.Tokenizer;
import org.apache.lucene.analysis.core.LowerCaseFilter;
import org.apache.lucene.analysis.en.PorterStemFilter;
import org.apache.lucene.analysis.standard.StandardTokenizer;
import org.apache.lucene.analysis.synonym.SynonymGraphFilter;
import org.apache.lucene.analysis.synonym.SynonymMap;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

import ca.uwindsor.common.Constants;
import org.apache.lucene.util.CharsRef;

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
     * If this should look for key terms only.
     */
    private final boolean keyTermsOnly;

    /**
     * Loaded stems.
     */
    private final TreeSet<String> stems;

    /**
     * Loaded keywords.
     */
    private final TreeSet<String> keywords;

    /**
     * Loaded abbreviations.
     */
    private SynonymMap abbreviations;

    /**
     * Set up the custom analyzer.
     *
     * @param keyTermsOnly Whether this analyzer should only use key terms or not.
     */
    public ComputerScienceAnalyzer(Boolean keyTermsOnly)
    {
        // Store if we are only interested in key terms.
        this.keyTermsOnly = keyTermsOnly;

        // Load the stems.
        stems = new TreeSet<>();
        TermsCollection.Load(stems, Constants.STEMS_FILE);

        // Load the keywords which will still need parsing.
        TreeSet<String> initialKeywords = new TreeSet<>();
        TermsCollection.Load(initialKeywords, Constants.KEY_TERMS);

        // Build the actual keywords and abbreviations mapping.
        keywords = new TreeSet<>();
        SynonymMap.Builder builder = new SynonymMap.Builder(true);
        for (String keyword : initialKeywords)
        {
            // Keywords split on the pipe symbol represent an abbreviation.
            String[] splits = keyword.split("\\|");
            splits[0] = splits[0].trim();

            // Check if something stems this keyword.
            keyword = TermsCollection.StringStartsWith(stems, splits[0], false);

            // If there is no stem, use the keyword itself.
            if (keyword == null)
            {
                keyword = splits[0];
            }

            // If there is a map, the keyword is actually just the abbreviation and map this to the term.
            if (splits.length > 1)
            {
                String abbreviation = splits[1].trim();
                keywords.add(abbreviation);
                builder.add(new CharsRef(keyword), new CharsRef(abbreviation), true);
            } else
            {
                keywords.add(keyword);
            }
        }

        try
        {
            abbreviations = builder.build();
        } catch (IOException e)
        {
            logger.error(e);
        }
    }

    /**
     * Determine how we should tokenize a field.
     *
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

        // Perform our custom stemming followed by the porter stemmer on top of it.
        tokenStream = new PorterStemFilter(new CustomStemFilter(tokenStream, stems));

        // Reduce any keywords to their abbreviations.
        tokenStream = new SynonymGraphFilter(tokenStream, abbreviations, true);

        // If only interested in the keywords, filter it to just them.
        if (keyTermsOnly)
        {
            return new TokenStreamComponents(tokenizer, new KeyTermsFilter(tokenStream, keywords));
        }

        // Otherwise, return everything.
        return new TokenStreamComponents(tokenizer, tokenStream);
    }

    /**
     * A function used for debugging that will return the transformed text.
     *
     * @param fieldName The name of the field to read the text of.
     * @param text      The text itself.
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