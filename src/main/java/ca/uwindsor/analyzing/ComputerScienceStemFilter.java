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
import org.apache.lucene.analysis.en.PorterStemFilter;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

import ca.uwindsor.sample_apps.StemmerSample;


public class ComputerScienceStemFilter extends TokenFilter {
	// the logger used for this class
	private static final Logger logger = LogManager.getLogger(ComputerScienceStemFilter.class);

    private final CharTermAttribute termAttr;
    
    private final PorterStemFilter porterStemFilter;    // this class provides basic stemming features
    private final Set<String> csStems;  // this set holds the stems

    public ComputerScienceStemFilter(TokenStream input, String stemsFilePath){
        super(input);
		termAttr = addAttribute(CharTermAttribute.class);
        this.porterStemFilter = new PorterStemFilter(input);
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

    // returns a stemmed version of term or throws if term does not match and csStems
    private String stemCSToken(String term) throws StemNotFoundException {
        for (String csStem : csStems) {
            if (csStem.startsWith(term)) {
                return csStem; // Found a string that starts with the prefix
            }
        }
        throw new StemNotFoundException("No stems found starting with: " + term); // No strings found that start with the prefix
    }


    @Override
    public final boolean incrementToken() throws IOException {
        String term = termAttr.toString();
        // Check if the term is in the custom stems set

        if (term == null || term.isEmpty()) {
            logger.debug("StemFilter Reached End of Stream");
            return false; // No valid term to process
        }

        try {
            String stemmedTerm = stemCSToken(term);
            // Set the modified term back to termAttr
            termAttr.setEmpty().append(stemmedTerm); // Set the new term
            
            logger.debug("Replaced \"" + term + "\" with \"" + stemmedTerm + "\".");

            return true;
        } catch (StemNotFoundException e) {
            // Apply Porter stemming if not a custom stem
            logger.trace("Porter Stemmed \"" + term + "\".");
            return porterStemFilter.incrementToken();
        }
    }
}
