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

    /*
     * Document Field Names
     */
    public enum FieldNames {
        PATH("path"),   // the path to this document on the server
        TITLE("title"), // the title of the document
        CONTENTS("contents"),   // the contents of the document
        STEMMED_CONTENTS("stemmed_contents"),   // the stemmed contents of the document
        KEYWORDS("keywords");   // the number of keywords in the document
    
        private final String value; // the instance of this enum that the instaniated enum is holding
    
        FieldNames(String value) {
            this.value = value;
        }
    
        public String getValue() {  // the string contents of this enum
            return value;
        }
    }
}
