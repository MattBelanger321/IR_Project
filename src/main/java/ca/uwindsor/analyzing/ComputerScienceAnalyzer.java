package ca.uwindsor.analyzing;

import java.io.IOException;
import java.io.StringReader;

import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.Tokenizer;
import org.apache.lucene.analysis.core.LowerCaseFilter;
import org.apache.lucene.analysis.en.PorterStemFilter;
import org.apache.lucene.analysis.standard.StandardTokenizer;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

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
        return new TokenStreamComponents(tokenizer, new PorterStemFilter( new ComputerScienceStemFilter( new LowerCaseFilter(tokenizer) , Constants.STEMS_FILE)));
    }

    // this is a function used for debugging that will return the transformed text 
    public String analyzeText(String fieldName, String text) throws IOException {
        StringBuilder analyzedText = new StringBuilder();
        
        // Tokenize the text with the same Analyzer used at index time
        try (TokenStream tokenStream = this.tokenStream(fieldName, new StringReader(text))) {
            CharTermAttribute charTermAttr = tokenStream.addAttribute(CharTermAttribute.class);
            tokenStream.reset();

            // Collect all tokens (transformed/analyzed version of the text)
            while (tokenStream.incrementToken()) {
                analyzedText.append(charTermAttr.toString()).append(" ");
            }

            tokenStream.end();
        }
        
        return analyzedText.toString().trim();
    }

}