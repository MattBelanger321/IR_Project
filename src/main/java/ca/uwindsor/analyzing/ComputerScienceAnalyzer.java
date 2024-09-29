package ca.uwindsor.analyzing;

import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.Tokenizer;
import org.apache.lucene.analysis.core.LowerCaseFilter;
import org.apache.lucene.analysis.standard.StandardTokenizer;

import ca.uwindsor.common.Constants;

/**
 * Capture specific terms only as tokens.
 */
public class ComputerScienceAnalyzer extends Analyzer
{
    /**
     * Determine how we should tokenize a field.
     * @param fieldName The field to tokenize which we do not use.
     * @return The keywords only tokenizer if we are tracking keywords, otherwise extend the standard with keywords.
     */
    @Override
    protected TokenStreamComponents createComponents(String fieldName)
    {
        Tokenizer tokenizer = new StandardTokenizer();

        // Ensure we are in lowercase.
        return new TokenStreamComponents(tokenizer, new ComputerScienceStemFilter(new LowerCaseFilter(tokenizer), Constants.STEMS_FILE));
    }
}