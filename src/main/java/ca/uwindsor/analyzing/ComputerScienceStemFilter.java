package ca.uwindsor.analyzing;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.util.HashSet;
import java.util.Set;

import org.apache.lucene.analysis.TokenFilter;
import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.en.PorterStemFilter;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;


public class ComputerScienceStemFilter extends TokenFilter {
    private final CharTermAttribute termAttr;
    
    private final PorterStemFilter porterStemFilter;    // this class provides basic stemming features
    private final Set<String> csStems;  // this set holds the stems

    public ComputerScienceStemFilter(TokenStream input, String stemsFilePath) throws IOException {
        super(input);
		termAttr = addAttribute(CharTermAttribute.class);
        this.porterStemFilter = new PorterStemFilter(input);
        this.csStems = new HashSet<>();
        initCustomStems(stemsFilePath);
    }

	// read stems from stems.txt
    private void initCustomStems(String stemsFilePath) throws IOException {
        try (BufferedReader reader = new BufferedReader(new FileReader(stemsFilePath))) {
            String line;
            while ((line = reader.readLine()) != null) {
                csStems.add(line.trim()); // Add each stem after trimming whitespace
            }
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
        try {
            term = stemCSToken(term);
            // Set the modified term back to termAttr
            termAttr.setEmpty().append(term); // Set the new term

            return true;
        } catch (StemNotFoundException e) {
            // Apply Porter stemming if not a custom stem
            return porterStemFilter.incrementToken();
        }
    }
}
