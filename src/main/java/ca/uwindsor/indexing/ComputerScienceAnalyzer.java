package ca.uwindsor.indexing;

import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.standard.StandardTokenizer;
import org.apache.lucene.analysis.synonym.SynonymGraphFilter;
import org.apache.lucene.analysis.synonym.SynonymMap;
import org.apache.lucene.util.CharsRef;

import java.io.IOException;

import ca.uwindsor.common.Constants;

/**
 * Allow for ensuring we capture computer science terms as single tokens.
 */
public class ComputerScienceAnalyzer extends Analyzer
{
    /**
     * The map of all computer science terms we want to keep as singular tokens.
     */
    private final SynonymMap synonymMap;

    /**
     * Load the computer science terms.
     * @throws IOException If there is an error loading the computer science terms.
     */
    public ComputerScienceAnalyzer() throws IOException
    {
        // Create the builder for the terms.
        SynonymMap.Builder builder = new SynonymMap.Builder(true);
        // Open the file containing the terms.
        for (String term : Constants.getTerms())
        {
            builder.add(new CharsRef(term.replaceAll(" ", "_")), new CharsRef(term), true);
        }
        // Save the map for use.
        this.synonymMap = builder.build();
    }

    /**
     * Create the tokens including our custom terms.
     * 
     * @param fieldName As far as I can tell, this is the text that is being
     *                  tokenized.
     * @return The tokens.
     */
    @Override
    protected TokenStreamComponents createComponents(String fieldName)
    {
        // Use the standard tokenizer as a base.
        StandardTokenizer src = new StandardTokenizer();
        // Add in our key terms to be tokenized.
        return new TokenStreamComponents(src, new SynonymGraphFilter(src, synonymMap, true));
    }
}
