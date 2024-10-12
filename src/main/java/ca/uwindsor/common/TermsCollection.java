package ca.uwindsor.common;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;

import java.io.BufferedReader;
import java.io.FileReader;
import java.io.IOException;
import java.util.Collection;
import java.util.TreeSet;

/**
 * Store a collection of terms with some useful functions.
 */
public class TermsCollection
{
    /**
     * The logger used for this class.
     */
    private static final Logger logger = LogManager.getLogger(TermsCollection.class);

    /**
     * Store the terms.
     */
    private final Collection<String> collection;

    /**
     * Do not add any terms and creates a tree set to store term.
     */
    public TermsCollection()
    {
        this.collection = new TreeSet<>();
    }

    /**
     * Start with an existing collection.
     *
     * @param collection   The starting collection.
     */
    public TermsCollection(Collection<String> collection)
    {
        this.collection = collection;
    }

    /**
     * Add terms from a file which will be normalized into a tree set.
     *
     * @param path  The path to the file.
     */
    public TermsCollection(String path)
    {
        this.collection = new TreeSet<>();
        Load(path, true);
    }

    /**
     * Add terms from a file into a tree set.
     *
     * @param path      The path to the file.
     * @param normalize If the terms should be normalized or not.
     */
    public TermsCollection(String path, Boolean normalize)
    {
        this.collection = new TreeSet<>();
        Load(path, normalize);
    }

    /**
     * Add terms from a file which will be normalized into an existing collection.
     *
     * @param collection   The starting collection.
     * @param path  The path to the file.
     */
    public TermsCollection(Collection<String> collection, String path)
    {
        this.collection = collection;
        Load(path, true);
    }

    /**
     * Add terms from a file into an existing collection.
     *
     * @param collection       The starting collection.
     * @param path      The path to the file.
     * @param normalize If the terms should be normalized or not.
     */
    public TermsCollection(Collection<String> collection, String path, Boolean normalize)
    {
        this.collection = collection;
        Load(path, normalize);
    }

    /**
     * Add normalized terms from a file.
     *
     * @param collection  The collection to add terms to.
     * @param path The path to the file.
     * @return The number of terms which were added.
     */
    public static int Load(Collection<String> collection, String path)
    {
        return Load(collection, path, true);
    }

    /**
     * Add terms from a file.
     *
     * @param collection       The collection to add terms to.
     * @param path      The path to the file.
     * @param normalize If the terms should be normalized or not.
     * @return The number of terms which were added.
     */
    public static int Load(Collection<String> collection, String path, boolean normalize)
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
                if (Add(collection, term, normalize))
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
        return Load(collection, path, true);
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
        return Load(collection, path, normalize);
    }

    /**
     * Add normalized terms from an iterable.
     *
     * @param collection        The collection to add terms to.
     * @param iterable The iterable to add.
     * @return The number of terms which were added.
     */
    public static int Add(Collection<String> collection, Iterable<String> iterable)
    {
        return Add(collection, iterable, true);
    }

    /**
     * Add terms from an iterable.
     *
     * @param collection        The collection to add terms to.
     * @param iterable The iterable to add.
     * @param normalize  If the terms should be normalized or not.
     * @return The number of terms which were added.
     */
    public static int Add(Collection<String> collection, Iterable<String> iterable, boolean normalize)
    {
        int count = 0;
        for (String term : iterable)
        {
            if (Add(collection, term, normalize))
            {
                count++;
            }
        }

        return count;
    }

    /**
     * Add normalized terms from an iterable.
     *
     * @param iterable The iterable to add.
     * @return The number of terms which were added.
     */
    public int Add(Iterable<String> iterable)
    {
        return Add(collection, iterable, true);
    }

    /**
     * Add terms from n iterable.
     *
     * @param iterable The iterable to add.
     * @param normalize  If the terms should be normalized or not.
     * @return The number of terms which were added.
     */
    public int Add(Iterable<String> iterable, boolean normalize)
    {
        return Add(collection, iterable, normalize);
    }

    /**
     * Add a normalized term.
     *
     * @param collection  The collection to add the term to.
     * @param term The term to add.
     * @return The number of terms which were added.
     */
    public static boolean Add(Collection<String> collection, String term)
    {
        return Add(collection, term, true);
    }

    /**
     * Add a term.
     *
     * @param collection       The collection to add the term to.
     * @param term      The term to add.
     * @param normalize If the term should be normalized or not.
     * @return The number of terms which were added.
     */
    public static boolean Add(Collection<String> collection, String term, boolean normalize)
    {
        term = term.trim();
        if (term.isEmpty())
        {
            return false;
        }

        return collection.add(normalize ? term.toLowerCase() : term);
    }

    /**
     * Add a normalized term.
     *
     * @param term The term to add.
     * @return The number of terms which were added.
     */
    public boolean Add(String term)
    {
        return Add(collection, term, true);
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
        return Add(collection, term, normalize);
    }

    /**
     * Remove terms from an iterable.
     *
     * @param collection        The collection to remove the terms from.
     * @param iterable The iterable to remove.
     * @param normalize  If the term should be normalized or not.
     * @return The number of terms which were removed.
     */
    public static int Remove(Collection<String> collection, Iterable<String> iterable, boolean normalize)
    {
        int count = 0;
        for (String term : iterable)
        {
            if (Remove(collection, term, normalize))
            {
                count++;
            }
        }

        return count;
    }

    /**
     * Remove a normalized term.
     *
     * @param collection  The collection to remove the term from.
     * @param term The term to remove.
     * @return True if the term was removed, false otherwise.
     */
    public static boolean Remove(Collection<String> collection, String term)
    {
        return Remove(collection, term, true);
    }

    /**
     * Remove a term.
     *
     * @param collection       The collection to remove the term from.
     * @param term      The term to remove.
     * @param normalize If the term should be normalized or not.
     * @return True if the term was removed, false otherwise.
     */
    public static boolean Remove(Collection<String> collection, String term, boolean normalize)
    {
        return collection.remove(normalize ? term.toLowerCase() : term);
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
        return Remove(collection, term, normalize);
    }

    /**
     * Remove a term.
     *
     * @param term The term to remove.
     * @return True if the term was removed, false otherwise.
     */
    public boolean Remove(String term)
    {
        return Remove(collection, term, true);
    }

    /**
     * Clear all terms.
     *
     * @param collection The collection to clear.
     * @return The number of terms that were in the collection before clearing it.
     */
    public static int Clear(Collection<String> collection)
    {
        int count = collection.size();
        collection.clear();
        return count;
    }

    /**
     * Clear all terms.
     *
     * @return The number of terms that were in the collection before clearing it.
     */
    public int Clear()
    {
        return Clear(collection);
    }

    /**
     * Get the number of terms in the collection.
     *
     * @return The number of terms in the collection.
     */
    public int Size()
    {
        return collection.size();
    }

    /**
     * Check if the collection contains a normalized term.
     *
     * @param collection  The collection.
     * @param term The term to look for.
     * @return True if the term is in the collection, false otherwise.
     */
    public static boolean Contains(Collection<String> collection, String term)
    {
        return Contains(collection, term, true);
    }

    /**
     * Check if the collection contains a term.
     *
     * @param collection       The collection.
     * @param term      The term to look for.
     * @param normalize If the term should be normalized or not.
     * @return True if the term is in the collection, false otherwise.
     */
    public static boolean Contains(Collection<String> collection, String term, boolean normalize)
    {
        if (normalize)
        {
            term = term.toLowerCase();
        }

        return collection.contains(term);
    }

    /**
     * Check if the collection contains a normalized term.
     *
     * @param term The term to look for.
     * @return True if the term is in the collection, false otherwise.
     */
    public boolean Contains(String term)
    {
        return Contains(collection, term, true);
    }

    /**
     * Check if the collection contains a term.
     *
     * @param term      The term to look for.
     * @param normalize If the term should be normalized or not.
     * @return True if the term is in the collection, false otherwise.
     */
    public boolean Contains(String term, boolean normalize)
    {
        return Contains(collection, term, normalize);
    }

    /**
     * Check if a term in the collection starts with a normalized string.
     *
     * @param collection        The collection.
     * @param startsWith The starting string to look for.
     * @return The first term which starts with the string if found, otherwise null.
     */
    public static String SetStartsWith(Collection<String> collection, String startsWith)
    {
        return SetStartsWith(collection, startsWith, true);
    }

    /**
     * Check if a term in the collection starts with a string.
     *
     * @param collection        The collection.
     * @param startsWith The starting string to look for.
     * @param normalize  If the string should be normalized or not.
     * @return The first term which starts with the string if found, otherwise null.
     */
    public static String SetStartsWith(Collection<String> collection, String startsWith, boolean normalize)
    {
        if (normalize)
        {
            startsWith = startsWith.toLowerCase();
        }

        for (String term : collection)
        {
            if (term.startsWith(startsWith))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a term in the collection starts with a normalized string.
     *
     * @param startsWith The starting string to look for.
     * @return The first term which starts with the string if found, otherwise null.
     */
    public String SetStartsWith(String startsWith)
    {
        return SetStartsWith(collection, startsWith, true);
    }

    /**
     * Check if a term in the collection starts with a string.
     *
     * @param startsWith The starting string to look for.
     * @param normalize  If the string should be normalized or not.
     * @return The first term which starts with the string if found, otherwise null.
     */
    public String SetStartsWith(String startsWith, boolean normalize)
    {
        return SetStartsWith(collection, startsWith, normalize);
    }

    /**
     * Check if a term in the collection ends with a normalized string.
     *
     * @param collection      The collection.
     * @param endsWith The ending string to look for.
     * @return The first term which ends with the string if found, otherwise null.
     */
    public static String SetEndsWith(Collection<String> collection, String endsWith)
    {
        return SetStartsWith(collection, endsWith, true);
    }

    /**
     * Check if a term in the collection ends with a string.
     *
     * @param collection       The collection.
     * @param endsWith  The ending string to look for.
     * @param normalize If the ending should be normalized or not.
     * @return The first term which ends with the string if found, otherwise null.
     */
    public static String SetEndsWith(Collection<String> collection, String endsWith, boolean normalize)
    {
        if (normalize)
        {
            endsWith = endsWith.toLowerCase();
        }

        for (String term : collection)
        {
            if (term.endsWith(endsWith))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a term in the collection ends with a normalized string.
     *
     * @param endsWith The ending string to look for.
     * @return The first term which ends with the string if found, otherwise null.
     */
    public String SetEndsWith(String endsWith)
    {
        return SetStartsWith(collection, endsWith, true);
    }

    /**
     * Check if a term in the collection ends with a string.
     *
     * @param endsWith  The ending string to look for.
     * @param normalize If the string should be normalized or not.
     * @return The first term which ends with the string if found, otherwise null.
     */
    public String SetEndsWith(String endsWith, boolean normalize)
    {
        return SetStartsWith(collection, endsWith, normalize);
    }

    /**
     * Check if a term in the collection contains a normalized string.
     *
     * @param collection       The collection.
     * @param substring The substring string to look for.
     * @return The first term which contains the substring if found, otherwise null.
     */
    public static String SetSubstring(Collection<String> collection, String substring)
    {
        return SetStartsWith(collection, substring, true);
    }

    /**
     * Check if a term in the collection contains a string.
     *
     * @param collection       The collection.
     * @param substring The substring string to look for.
     * @param normalize If the ending should be normalized or not.
     * @return The first term which contains the substring if found, otherwise null.
     */
    public static String SetSubstring(Collection<String> collection, String substring, boolean normalize)
    {
        if (normalize)
        {
            substring = substring.toLowerCase();
        }

        for (String term : collection)
        {
            if (term.contains(substring))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a term in the collection contains a normalized string.
     *
     * @param substring The substring string to look for.
     * @return The first term which contains the substring if found, otherwise null.
     */
    public String SetSubstring(String substring)
    {
        return SetStartsWith(collection, substring, true);
    }

    /**
     * Check if a term in the collection contains a string.
     *
     * @param substring The substring string to look for.
     * @param normalize If the string should be normalized or not.
     * @return The first term which contains the substring if found, otherwise null.
     */
    public String SetSubstring(String substring, boolean normalize)
    {
        return SetStartsWith(collection, substring, normalize);
    }

    /**
     * Check if a normalized string starts with any term in a collection.
     *
     * @param collection    The collection.
     * @param string The string.
     * @return The first term in the collection which the string starts with if one is found, otherwise null.
     */
    public static String StringStartsWith(Collection<String> collection, String string)
    {
        return StringStartsWith(collection, string, true);
    }

    /**
     * Check if a string starts with any term in a collection.
     *
     * @param collection       The collection.
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the collection which the string starts with if one is found, otherwise null.
     */
    public static String StringStartsWith(Collection<String> collection, String string, boolean normalize)
    {
        if (normalize)
        {
            string = string.toLowerCase();
        }

        for (String term : collection)
        {
            if (string.startsWith(term))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a normalized string starts with any term in a collection.
     *
     * @param string The string.
     * @return The first term in the collection which the string starts with if one is found, otherwise null.
     */
    public String StringStartsWith(String string)
    {
        return StringStartsWith(collection, string, true);
    }

    /**
     * Check if a string starts with any term in a collection.
     *
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the collection which the string starts with if one is found, otherwise null.
     */
    public String StringStartsWith(String string, boolean normalize)
    {
        return StringStartsWith(collection, string, normalize);
    }

    /**
     * Check if a normalized string ends with any term in a collection.
     *
     * @param collection    The collection.
     * @param string The string.
     * @return The first term in the collection which the string ends with if one is found, otherwise null.
     */
    public static String StringEndsWith(Collection<String> collection, String string)
    {
        return StringEndsWith(collection, string, true);
    }

    /**
     * Check if a string ends with any term in a collection.
     *
     * @param collection       The collection.
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the collection which the string ends with if one is found, otherwise null.
     */
    public static String StringEndsWith(Collection<String> collection, String string, boolean normalize)
    {
        if (normalize)
        {
            string = string.toLowerCase();
        }

        for (String term : collection)
        {
            if (string.endsWith(term))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a normalized string ends with any term in a collection.
     *
     * @param string The string.
     * @return The first term in the collection which the string ends with if one is found, otherwise null.
     */
    public String StringEndsWith(String string)
    {
        return StringEndsWith(collection, string, true);
    }

    /**
     * Check if a string ends with any term in a collection.
     *
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the collection which the string ends with if one is found, otherwise null.
     */
    public String StringEndsWith(String string, boolean normalize)
    {
        return StringEndsWith(collection, string, normalize);
    }

    /**
     * Check if a normalized string substrings any term in a collection.
     *
     * @param collection    The collection.
     * @param string The string.
     * @return The first term in the collection which the string substrings if one is found, otherwise null.
     */
    public static String StringSubstring(Collection<String> collection, String string)
    {
        return StringSubstring(collection, string, true);
    }

    /**
     * Check if a string substrings any term in a collection.
     *
     * @param collection       The collection.
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the collection which the string substrings if one is found, otherwise null.
     */
    public static String StringSubstring(Collection<String> collection, String string, boolean normalize)
    {
        if (normalize)
        {
            string = string.toLowerCase();
        }

        for (String term : collection)
        {
            if (string.contains(term))
            {
                return term;
            }
        }

        return null;
    }

    /**
     * Check if a normalized string substrings any term in a collection.
     *
     * @param string The string.
     * @return The first term in the collection which the string substrings if one is found, otherwise null.
     */
    public String StringSubstring(String string)
    {
        return StringSubstring(collection, string, true);
    }

    /**
     * Check if a string substrings any term in a collection.
     *
     * @param string    The string.
     * @param normalize If the string should be normalized or not.
     * @return The first term in the collection which the string substrings if one is found, otherwise null.
     */
    public String StringSubstring(String string, boolean normalize)
    {
        return StringSubstring(collection, string, normalize);
    }
}