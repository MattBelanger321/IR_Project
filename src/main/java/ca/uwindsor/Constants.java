package ca.uwindsor;

/**
 * Store constants for the project.
 */
public class Constants
{
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
     * How many of the starting lines (after the first for the title) should be indexed.
     * This can be used to help capture author names or keywords for instance.
     */
    public static final int indexedLines = 10;
}
