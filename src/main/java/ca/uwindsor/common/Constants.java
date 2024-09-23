package ca.uwindsor.common;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.util.HashSet;

/**
 * Store constants for the project.
 */
public class Constants {
    /**
     * The name of the data to use.
     */
    public static final String data = "citeceer2";

    /**
     * The data index folder.
     */
    public static final String dataIndex = data + "_index";

    /**
     * The file containing key computer science terms we wish to tokenize with.
     */
    public static final String keyTerms = "terms.txt";

    /**
     * How many of the starting lines (after the first for the title) should be
     * indexed.
     * This can be used to help capture author names or keywords for instance.
     */
    public static final int indexedLines = 10;

    /**
     * Store the computer science terms.
     */
    private static HashSet<String> terms = null;

    /**
     * Get the computer science terms.
     * @return The computer science terms.
     * @throws IOException If there is an error loading the computer science terms.
     */
    public static HashSet<String> getTerms() throws IOException
    {
        // If the terms have not yet been loaded, load them.
        if (terms == null)
        {
            terms = new HashSet<>();
            BufferedReader br = new BufferedReader(new FileReader(keyTerms));
            // Read the file, with a new term on each line.
            String line;
            while ((line = br.readLine()) != null)
            {
                terms.add(line.trim().toLowerCase());
            }
            br.close();
        }

        return terms;
    }
}
