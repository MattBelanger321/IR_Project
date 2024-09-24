package ca.uwindsor.analyzing;

import ca.uwindsor.common.Constants;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.Tokenizer;
import org.apache.lucene.analysis.core.WhitespaceTokenizer;
import org.apache.lucene.analysis.standard.StandardTokenizer;
import org.apache.lucene.analysis.synonym.SynonymGraphFilter;
import org.apache.lucene.analysis.synonym.SynonymMap;
import org.apache.lucene.util.CharsRef;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.util.HashSet;

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
     * Store the computer science terms.
     */
    private static HashSet<String> terms = null;

    /**
     * The base tokenizer to use.
     */
    private final Tokenizer tokenizer;

    /**
     * Load the computer science terms.
     * @throws IOException If there is an error loading the computer science terms.
     */
    public ComputerScienceAnalyzer(Boolean keyTermsOnly) throws IOException
    {
        // Set the tokenizer.
        tokenizer = keyTermsOnly ? new WhitespaceTokenizer() : new StandardTokenizer();

        // If the terms have not yet been loaded, load them.
        if (terms == null)
        {
            terms = new HashSet<>();
            BufferedReader br = new BufferedReader(new FileReader(Constants.keyTerms));
            // Read the file, with a new term on each line.
            String line;
            while ((line = br.readLine()) != null)
            {
                terms.add(line.trim().toLowerCase());
            }
            br.close();
        }

        // Create the builder for the terms.
        SynonymMap.Builder builder = new SynonymMap.Builder(true);
        // Open the file containing the terms.
        for (String term : terms)
        {
            term = term.toLowerCase();
            builder.add(new CharsRef(term), new CharsRef(term), true);
        }
        // Save the map for use.
        this.synonymMap = builder.build();
    }

    /**
     * Create the tokens including our custom terms.
     * @param fieldName The name of the field in case that changes what we do here, which in our case does not.
     * @return The tokens.
     */
    @Override
    protected TokenStreamComponents createComponents(String fieldName)
    {
        // Add in our key terms to be tokenized.
        return new TokenStreamComponents(tokenizer, new SynonymGraphFilter(tokenizer, synonymMap, true));
    }
}