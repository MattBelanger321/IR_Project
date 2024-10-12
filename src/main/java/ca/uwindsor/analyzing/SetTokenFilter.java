package ca.uwindsor.analyzing;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.lucene.analysis.TokenFilter;
import org.apache.lucene.analysis.TokenStream;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.util.Collection;
import java.util.HashSet;

/**
 * Implement a filter that has a collection of terms.
 */
public abstract class SetTokenFilter extends TokenFilter
{
    /**
     * The logger used for this class.
     */
    private static final Logger logger = LogManager.getLogger(SetTokenFilter.class);

    /**
     * The terms stored in this set.
     */
    private final Collection<String> set;

    /**
     * Do not add any terms and creates a hash set to store term.
     *
     * @param input The input stream.
     */
    public SetTokenFilter(TokenStream input)
    {
        super(input);
        this.set = new HashSet<>();
    }

    /**
     * Start with an existing set.
     *
     * @param input The input stream.
     * @param set   The starting set.
     */
    public SetTokenFilter(TokenStream input, Collection<String> set)
    {
        super(input);
        this.set = set;
    }

    /**
     * Add terms from a file which will be normalized into a hash set.
     *
     * @param input The input stream.
     * @param path  The path to the file.
     */
    public SetTokenFilter(TokenStream input, String path)
    {
        super(input);
        this.set = new HashSet<>();
        Load(path, true);
    }

    /**
     * Add terms from a file into a hash set.
     *
     * @param input     The input stream.
     * @param path      The path to the file.
     * @param normalize If the terms should be normalized or not.
     */
    public SetTokenFilter(TokenStream input, String path, Boolean normalize)
    {
        super(input);
        this.set = new HashSet<>();
        Load(path, normalize);
    }

    /**
     * Add terms from a file which will be normalized into an existing set.
     *
     * @param input The input stream.
     * @param set   The starting set.
     * @param path  The path to the file.
     */
    public SetTokenFilter(TokenStream input, Collection<String> set, String path)
    {
        super(input);
        this.set = set;
        Load(path, true);
    }

    /**
     * Add terms from a file into an existing set.
     *
     * @param input     The input stream.
     * @param set       The starting set.
     * @param path      The path to the file.
     * @param normalize If the terms should be normalized or not.
     */
    public SetTokenFilter(TokenStream input, Collection<String> set, String path, Boolean normalize)
    {
        super(input);
        this.set = set;
        Load(path, normalize);
    }

    /**
     * Add normalized terms from a file.
     *
     * @param set  The set to add terms to.
     * @param path The path to the file.
     * @return The number of terms which were added.
     */
    public static int Load(Collection<String> set, String path)
    {
        return Load(set, path, true);
    }

    /**
     * Add terms from a file.
     *
     * @param set       The set to add terms to.
     * @param path      The path to the file.
     * @param normalize If the terms should be normalized or not.
     * @return The number of terms which were added.
     */
    public static int Load(Collection<String> set, String path, boolean normalize)
    {
        // Open the file.
        int count = 0;
        try (BufferedReader reader = new BufferedReader(new FileReader(path)))
        {
            // Every line is a term.
            String term;
            while ((term = reader.readLine()) != null)
            {
                // Try to add every line.
                if (Add(set, term, normalize))
                {
                    count++;
                }
            }
        } catch (IOException e)
        {
            logger.error(e);
        }

        return count;
    }

    /**
     * Add normalized terms from a file.
     *
     * @param path The path to the file.
     * @return The number of terms which were added.
     */
    public int Load(String path)
    {
        return Load(set, path, true);
    }

    /**
     * Add terms from a file.
     *
     * @param path      The path to the file.
     * @param normalize If the terms should be normalized or not.
     * @return The number of terms which were added.
     */
    public int Load(String path, Boolean normalize)
    {
        return Load(set, path, normalize);
    }

    /**
     * Add normalized terms from a collection.
     *
     * @param set        The set to add terms to.
     * @param collection The collection to add.
     * @return The number of terms which were added.
     */
    public static int Add(Collection<String> set, Collection<String> collection)
    {
        return Add(set, collection, true);
    }

    /**
     * Add terms from a collection.
     *
     * @param set        The set to add terms to.
     * @param collection The collection to add.
     * @param normalize  If the terms should be normalized or not.
     * @return The number of terms which were added.
     */
    public static int Add(Collection<String> set, Collection<String> collection, boolean normalize)
    {
        int count = 0;
        for (String term : collection)
        {
            if (Add(set, term, normalize))
            {
                count++;
            }
        }

        return count;
    }

    /**
     * Add normalized terms from a collection.
     *
     * @param collection The collection to add.
     * @return The number of terms which were added.
     */
    public int Add(Collection<String> collection)
    {
        return Add(set, collection, true);
    }

    /**
     * Add terms from a collection.
     *
     * @param collection The collection to add.
     * @param normalize  If the terms should be normalized or not.
     * @return The number of terms which were added.
     */
    public int Add(Collection<String> collection, boolean normalize)
    {
        return Add(set, collection, normalize);
    }

    /**
     * Add a normalized term.
     *
     * @param set  The set to add the term to.
     * @param term The term to add.
     * @return The number of terms which were added.
     */
    public static boolean Add(Collection<String> set, String term)
    {
        return Add(set, term, true);
    }

    /**
     * Add a term.
     *
     * @param set       The set to add the term to.
     * @param term      The term to add.
     * @param normalize If the term should be normalized or not.
     * @return The number of terms which were added.
     */
    public static boolean Add(Collection<String> set, String term, boolean normalize)
    {
        term = term.trim();
        if (term.isEmpty())
        {
            return false;
        }

        return set.add(normalize ? term.toLowerCase() : term);
    }

    /**
     * Add a normalized term.
     *
     * @param term The term to add.
     * @return The number of terms which were added.
     */
    public boolean Add(String term)
    {
        return Add(set, term, true);
    }

    /**
     * Add a term.
     *
     * @param term      The term to add.
     * @param normalize If the term should be normalized or not.
     * @return The number of terms which were added.
     */
    public boolean Add(String term, boolean normalize)
    {
        return Add(set, term, normalize);
    }

    /**
     * Remove terms from a collection.
     *
     * @param set        The set to remove the terms from.
     * @param collection The collection to remove.
     * @param normalize  If the term should be normalized or not.
     * @return The number of terms which were removed.
     */
    public static int Remove(Collection<String> set, Collection<String> collection, boolean normalize)
    {
        int count = 0;
        for (String term : collection)
        {
            if (Remove(set, term, normalize))
            {
                count++;
            }
        }

        return count;
    }

    /**
     * Remove a normalized term.
     *
     * @param set  The set to remove the term from.
     * @param term The term to remove.
     * @return True if the term was removed, false otherwise.
     */
    public static boolean Remove(Collection<String> set, String term)
    {
        return Remove(set, term, true);
    }

    /**
     * Remove a term.
     *
     * @param set       The set to remove the term from.
     * @param term      The term to remove.
     * @param normalize If the term should be normalized or not.
     * @return True if the term was removed, false otherwise.
     */
    public static boolean Remove(Collection<String> set, String term, boolean normalize)
    {
        return set.remove(normalize ? term.toLowerCase() : term);
    }

    /**
     * Remove a normalized term.
     *
     * @param term      The term to remove.
     * @param normalize If the term should be normalized or not.
     * @return True if the term was removed, false otherwise.
     */
    public boolean Remove(String term, boolean normalize)
    {
        return Remove(set, term, normalize);
    }

    /**
     * Remove a term.
     *
     * @param term The term to remove.
     * @return True if the term was removed, false otherwise.
     */
    public boolean Remove(String term)
    {
        return Remove(set, term, true);
    }

    /**
     * Clear all terms.
     *
     * @param set The set to clear.
     * @return The number of terms that were in the set before clearing it.
     */
    public static int Clear(Collection<String> set)
    {
        int count = set.size();
        set.clear();
        return count;
    }

    /**
     * Clear all terms.
     *
     * @return The number of terms that were in the set before clearing it.
     */
    public int Clear()
    {
        return Clear(set);
    }

    /**
     * Get the number of terms in the set.
     *
     * @return The number of terms in the set.
     */
    public int Size()
    {
        return set.size();
    }

    /**
     * Check if the set contains a normalized term.
     *
     * @param set  The set.
     * @param term The term to look for.
     * @return True if the term is in the set, false otherwise.
     */
    public static boolean Contains(Collection<String> set, String term)
    {
        return Contains(set, term, true);
    }

    /**
     * Check if the set contains a term.
     *
     * @param set       The set.
     * @param term      The term to look for.
     * @param normalize If the term should be normalized or not.
     * @return True if the term is in the set, false otherwise.
     */
    public static boolean Contains(Collection<String> set, String term, boolean normalize)
    {
        if (normalize)
        {
            term = term.toLowerCase();
        }

        return set.contains(term);
    }

    /**
     * Check if the set contains a normalized term.
     *
     * @param term The term to look for.
     * @return True if the term is in the set, false otherwise.
     */
    public boolean Contains(String term)
    {
        return Contains(set, term, true);
    }

    /**
     * Check if the set contains a term.
     *
     * @param term      The term to look for.
     * @param normalize If the term should be normalized or not.
     * @return True if the term is in the set, false otherwise.
     */
    public boolean Contains(String term, boolean normalize)
    {
        return Contains(set, term, normalize);
    }

    /**
     * Check if a term in the set starts with a normalized string.
     *
     * @param set        The set.
     * @param startsWith The starting string to look for.
     * @return The first term which starts with the string if found, otherwise null.
     */
    public static String SetStartsWith(Collection<String> set, String startsWith)
    {
        return SetStartsWith(set, startsWith, true);
    }

    /**
     * Check if a term in the set starts with a string.
     *
     * @param set        The set.
     * @param startsWith The starting string to look for.
     * @param normalize  If the string should be normalized or not.
     * @return The first term which starts with the string if found, otherwise null.
     */
    public static String SetStartsWith(Collection<String> set, String startsWith, boolean normalize)
    {
        if (normalize)
        {
            startsWith = startsWith.toLowerCase();
        }

        for (String term : set)
        {
            if (term.startsWith(startsWith))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a term in the set starts with a normalized string.
     *
     * @param startsWith The starting string to look for.
     * @return The first term which starts with the string if found, otherwise null.
     */
    public String SetStartsWith(String startsWith)
    {
        return SetStartsWith(set, startsWith, true);
    }

    /**
     * Check if a term in the set starts with a string.
     *
     * @param startsWith The starting string to look for.
     * @param normalize  If the string should be normalized or not.
     * @return The first term which starts with the string if found, otherwise null.
     */
    public String SetStartsWith(String startsWith, boolean normalize)
    {
        return SetStartsWith(set, startsWith, normalize);
    }

    /**
     * Check if a term in the set ends with a normalized string.
     *
     * @param set      The set.
     * @param endsWith The ending string to look for.
     * @return The first term which ends with the string if found, otherwise null.
     */
    public static String SetEndsWith(Collection<String> set, String endsWith)
    {
        return SetStartsWith(set, endsWith, true);
    }

    /**
     * Check if a term in the set ends with a string.
     *
     * @param set       The set.
     * @param endsWith  The ending string to look for.
     * @param normalize If the ending should be normalized or not.
     * @return The first term which ends with the string if found, otherwise null.
     */
    public static String SetEndsWith(Collection<String> set, String endsWith, boolean normalize)
    {
        if (normalize)
        {
            endsWith = endsWith.toLowerCase();
        }

        for (String term : set)
        {
            if (term.endsWith(endsWith))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a term in the set ends with a normalized string.
     *
     * @param endsWith The ending string to look for.
     * @return The first term which ends with the string if found, otherwise null.
     */
    public String SetEndsWith(String endsWith)
    {
        return SetStartsWith(set, endsWith, true);
    }

    /**
     * Check if a term in the set ends with a string.
     *
     * @param endsWith  The ending string to look for.
     * @param normalize If the string should be normalized or not.
     * @return The first term which ends with the string if found, otherwise null.
     */
    public String SetEndsWith(String endsWith, boolean normalize)
    {
        return SetStartsWith(set, endsWith, normalize);
    }

    /**
     * Check if a term in the set contains a normalized string.
     *
     * @param set       The set.
     * @param substring The substring string to look for.
     * @return The first term which contains the substring if found, otherwise null.
     */
    public static String SetSubstring(Collection<String> set, String substring)
    {
        return SetStartsWith(set, substring, true);
    }

    /**
     * Check if a term in the set contains a string.
     *
     * @param set       The set.
     * @param substring The substring string to look for.
     * @param normalize If the ending should be normalized or not.
     * @return The first term which contains the substring if found, otherwise null.
     */
    public static String SetSubstring(Collection<String> set, String substring, boolean normalize)
    {
        if (normalize)
        {
            substring = substring.toLowerCase();
        }

        for (String term : set)
        {
            if (term.contains(substring))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a term in the set contains a normalized string.
     *
     * @param substring The substring string to look for.
     * @return The first term which contains the substring if found, otherwise null.
     */
    public String SetSubstring(String substring)
    {
        return SetStartsWith(set, substring, true);
    }

    /**
     * Check if a term in the set contains a string.
     *
     * @param substring The substring string to look for.
     * @param normalize If the string should be normalized or not.
     * @return The first term which contains the substring if found, otherwise null.
     */
    public String SetSubstring(String substring, boolean normalize)
    {
        return SetStartsWith(set, substring, normalize);
    }

    /**
     * Check if a normalized string starts with any term in a set.
     *
     * @param set    The set.
     * @param string The string.
     * @return The first term in the set which the string starts with if one is found, otherwise null.
     */
    public static String StringStartsWith(Collection<String> set, String string)
    {
        return StringStartsWith(set, string, true);
    }

    /**
     * Check if a string starts with any term in a set.
     *
     * @param set       The set.
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the set which the string starts with if one is found, otherwise null.
     */
    public static String StringStartsWith(Collection<String> set, String string, boolean normalize)
    {
        if (normalize)
        {
            string = string.toLowerCase();
        }

        for (String term : set)
        {
            if (string.startsWith(term))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a normalized string starts with any term in a set.
     *
     * @param string The string.
     * @return The first term in the set which the string starts with if one is found, otherwise null.
     */
    public String StringStartsWith(String string)
    {
        return StringStartsWith(set, string, true);
    }

    /**
     * Check if a string starts with any term in a set.
     *
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the set which the string starts with if one is found, otherwise null.
     */
    public String StringStartsWith(String string, boolean normalize)
    {
        return StringStartsWith(set, string, normalize);
    }

    /**
     * Check if a normalized string ends with any term in a set.
     *
     * @param set    The set.
     * @param string The string.
     * @return The first term in the set which the string ends with if one is found, otherwise null.
     */
    public static String StringEndsWith(Collection<String> set, String string)
    {
        return StringEndsWith(set, string, true);
    }

    /**
     * Check if a string ends with any term in a set.
     *
     * @param set       The set.
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the set which the string ends with if one is found, otherwise null.
     */
    public static String StringEndsWith(Collection<String> set, String string, boolean normalize)
    {
        if (normalize)
        {
            string = string.toLowerCase();
        }

        for (String term : set)
        {
            if (string.endsWith(term))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a normalized string ends with any term in a set.
     *
     * @param string The string.
     * @return The first term in the set which the string ends with if one is found, otherwise null.
     */
    public String StringEndsWith(String string)
    {
        return StringEndsWith(set, string, true);
    }

    /**
     * Check if a string ends with any term in a set.
     *
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the set which the string ends with if one is found, otherwise null.
     */
    public String StringEndsWith(String string, boolean normalize)
    {
        return StringEndsWith(set, string, normalize);
    }

    /**
     * Check if a normalized string substrings any term in a set.
     *
     * @param set    The set.
     * @param string The string.
     * @return The first term in the set which the string substrings if one is found, otherwise null.
     */
    public static String StringSubstring(Collection<String> set, String string)
    {
        return StringSubstring(set, string, true);
    }

    /**
     * Check if a string substrings any term in a set.
     *
     * @param set       The set.
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the set which the string substrings if one is found, otherwise null.
     */
    public static String StringSubstring(Collection<String> set, String string, boolean normalize)
    {
        if (normalize)
        {
            string = string.toLowerCase();
        }

        for (String term : set)
        {
            if (string.contains(term))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a normalized string substrings any term in a set.
     *
     * @param string The string.
     * @return The first term in the set which the string substrings if one is found, otherwise null.
     */
    public String StringSubstring(String string)
    {
        return StringSubstring(set, string, true);
    }

    /**
     * Check if a string substrings any term in a set.
     *
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the set which the string substrings if one is found, otherwise null.
     */
    public String StringSubstring(String string, boolean normalize)
    {
        return StringSubstring(set, string, normalize);
    }
}