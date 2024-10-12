package ca.uwindsor.common;

/**
 * Store constants for the project.
 */
public class Constants
{
    /**
     * The name of the data to use.
     */
    public static final String DATA = "citeceer2";

    /**
     * The data index folder.
     */
    public static final String DATA_INDEX = DATA + "_index";

    /**
     * The file containing key computer science terms we wish to tokenize with.
     */
    public static final String KEY_TERMS = "terms.txt";

    /**
     * The file containing key computer science stems.
     */
    public static final String STEMS_FILE = "stems.txt";

    /**
     * Document field names.
     */
    public enum FieldNames
    {
        /**
         * The path to this document on the server.
         */
        PATH("path"),

        /**
         * The title of the document.
         */
        TITLE("title"),

        /**
         * The contents of the document.
         */
        CONTENTS("contents"),

        /**
         * The stemmed contents of the document.
         */
        STEMMED_CONTENTS("stemmed_contents"),

        /**
         * The keywords in the document.
         */
        KEYWORDS("keywords");

        /**
         * The instance of this enum that the instantiated enum is holding.
         */
        private final String value;

        /**
         * Create a new field name.
         * @param value The value for it.
         */
        FieldNames(String value)
        {
            this.value = value;
        }

        /**
         * Get the string contents of this enum.
         */
        public String getValue()
        {
            return value;
        }
    }
}
