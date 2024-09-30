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


public class ComputerScienceStemFilter extends TokenFilter {
	// the logger used for this class
	private static final Logger logger = LogManager.getLogger(ComputerScienceStemFilter.class);

    private final CharTermAttribute termAttr;
    
    private final Set<String> csStems;  // this set holds the stems

    public ComputerScienceStemFilter(TokenStream input, String stemsFilePath){
        super(input);
		termAttr = addAttribute(CharTermAttribute.class);
        this.csStems = new HashSet<>();
        initCustomStems(stemsFilePath);
        logger.info("Constructed CS Stem Filter");
    }

	// read stems from stems.txt
    private void initCustomStems(String stemsFilePath) {
        try (BufferedReader reader = new BufferedReader(new FileReader(stemsFilePath))) {
            String line;
            while ((line = reader.readLine()) != null) {
                String stem = line.trim();
                csStems.add(stem); // Add each stem after trimming whitespace
                logger.debug("Added \"" + stem + "\" to stems list");
            }
            reader.close();
        } catch (IOException ex) {
            // ignored
        }
    }

    // returns a stemmed version of term or returns original term
    private String stemCSToken(String term)  {
        for (String csStem : csStems) {
            if (term.startsWith(csStem)) {
                return csStem; // Found a string that starts with the prefix
            }
        }
        return term;
    }


    @Override
    public final boolean incrementToken() throws IOException {

        // First, check if there is another token to process
        if (!input.incrementToken()) {
            logger.debug("StemFilter Reached End of Stream");
            return false; // No more tokens, end of stream
        }

        String term = termAttr.toString();
        // Check if the term is in the custom stems set

        if (term == null || term.isEmpty()) {
            logger.debug("StemFilter Reached End of Stream");
            return false; // No valid term to process
        }
      
        logger.trace("Checking term if \"" + term + "\" can be CS Stemmed");
        String stemmedTerm = stemCSToken(term);
        // Set the modified term back to termAttr
        termAttr.setEmpty().append(stemmedTerm); // Set the new term
        
        if(!stemmedTerm.equals(term))
            logger.trace("Replaced \"" + term + "\" with \"" + stemmedTerm + "\".");
        return true;
    }
}
