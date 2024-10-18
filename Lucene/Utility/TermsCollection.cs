namespace SearchEngine.Lucene.Utility;

/// <summary>
/// Helper method to make it easy to load and work with files of text.
/// </summary>
public class TermsCollection
{
    /// <summary>
    /// Get how many items are in the collection.
    /// </summary>
    public int Count => _collection.Count;
    
    /// <summary>
    /// The collection itself.
    /// </summary>
    private readonly ICollection<string> _collection;

    /// <summary>
    /// Create a new collection of a sorted set.
    /// </summary>
    public TermsCollection()
    {
        _collection = new SortedSet<string>();
    }

    /// <summary>
    /// Use an existing collection.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    public TermsCollection(ICollection<string> collection)
    {
        _collection = collection;
    }

    /// <summary>
    /// Load a file into a sorted set.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public TermsCollection(string path, bool normalize = true)
    {
        _collection = new SortedSet<string>();
        Load(_collection, path, normalize);
    }

    /// <summary>
    /// Load a file into an existing collection.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public TermsCollection(ICollection<string> collection, string path, bool normalize = true)
    {
        _collection = collection;
        Load(_collection, path, normalize);
    }

    /// <summary>
    /// Load a file into an existing collection.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="path">The path of the file.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public static void Load(ICollection<string> collection, string path, bool normalize = true)
    {
        foreach (string s in File.ReadLines(path))
        {
            Add(collection, s, normalize);
        }
    }

    /// <summary>
    /// Load a file into the collection.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public void Load(string path, bool normalize = true)
    {
        Load(_collection, path, normalize);
    }

    /// <summary>
    /// Add terms into a collection.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="iterable">The terms to add.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public static void Add(ICollection<string> collection, IEnumerable<string> iterable, bool normalize = true)
    {
        foreach (string s in iterable)
        {
            Add(collection, s, normalize);
        }
    }
    
    /// <summary>
    /// Add terms into the collection.
    /// </summary>
    /// <param name="iterable">The terms to add.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public void Add(IEnumerable<string> iterable, bool normalize = true)
    {
        Add(_collection, iterable, normalize);
    }

    /// <summary>
    /// Add a string into a collection.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="s">The string to add.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public static void Add(ICollection<string> collection, string s, bool normalize = true)
    {
        collection.Add(normalize ? s.ToLower() : s);
    }

    /// <summary>
    /// Add a string into the collection.
    /// </summary>
    /// <param name="s">The string to add.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public void Add(string s, bool normalize = true)
    {
        Add(_collection, s, normalize);
    }

    /// <summary>
    /// Remove terms from a collection.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="iterable">The terms to remove.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public static void Remove(ICollection<string> collection, IEnumerable<string> iterable, bool normalize = true)
    {
        foreach (string s in iterable)
        {
            Remove(collection, s, normalize);
        }
    }
    
    /// <summary>
    /// Remove terms from the collection.
    /// </summary>
    /// <param name="iterable">The terms to remove.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public void Remove(IEnumerable<string> iterable, bool normalize = true)
    {
        Remove(_collection, iterable, normalize);
    }

    /// <summary>
    /// Remove a string from a collection.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="s">The string to remove.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public static void Remove(ICollection<string> collection, string s, bool normalize = true)
    {
        collection.Remove(normalize ? s.ToLower() : s);
    }

    /// <summary>
    /// Remove a string from the collection.
    /// </summary>
    /// <param name="s">The string to remove.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    public void Remove(string s, bool normalize = true)
    {
        Remove(_collection, s, normalize);
    }

    /// <summary>
    /// Clear the collection.
    /// </summary>
    public void Clear()
    {
        _collection.Clear();
    }

    /// <summary>
    /// If a collection contains a string.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="s">The string to look for.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>True if it contains the string, false otherwise.</returns>
    public static bool Contains(ICollection<string> collection, string s, bool normalize = true)
    {
        return collection.Contains(normalize ? s.ToLower() : s);
    }
    
    /// <summary>
    /// If the collection contains a string.
    /// </summary>
    /// <param name="s">The string to look for.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>True if it contains the string, false otherwise.</returns>
    public bool Contains(string s, bool normalize = true)
    {
        return Contains(_collection, s, normalize);
    }

    /// <summary>
    /// If a collection contains a term which starts with a string.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="startsWith">The string to see if any term starts with it.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which starts with the string if one is found, null otherwise.</returns>
    public static string? CollectionStartsWith(ICollection<string> collection, string startsWith, bool normalize = true)
    {
        if (normalize)
        {
            startsWith = startsWith.ToLower();
        }

        return collection.FirstOrDefault(s => s.StartsWith(startsWith));
    }

    /// <summary>
    /// If the collection contains a term which starts with a string.
    /// </summary>
    /// <param name="startsWith">The string to see if any term starts with it.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which starts with the string if one is found, null otherwise.</returns>
    public string? CollectionStartsWith(string startsWith, bool normalize = true)
    {
        return CollectionStartsWith(_collection, startsWith, normalize);
    }

    /// <summary>
    /// If a collection contains a term which ends with a string.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="endsWith">The string to see if any term ends with it.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which ends with the string if one is found, null otherwise.</returns>
    public static string? CollectionEndsWith(ICollection<string> collection, string endsWith, bool normalize = true)
    {
        if (normalize)
        {
            endsWith = endsWith.ToLower();
        }

        return collection.FirstOrDefault(s => s.EndsWith(endsWith));
    }

    /// <summary>
    /// If the collection contains a term which ends with a string.
    /// </summary>
    /// <param name="endsWith">The string to see if any term ends with it.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which ends with the string if one is found, null otherwise.</returns>
    public string? CollectionEndsWith(string endsWith, bool normalize = true)
    {
        return CollectionEndsWith(_collection, endsWith, normalize);
    }

    /// <summary>
    /// If a collection contains a term which substrings a string.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="substring">The string to see if any term substrings it.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which substrings the string if one is found, null otherwise.</returns>
    public static string? CollectionSubstring(ICollection<string> collection, string substring, bool normalize = true)
    {
        if (normalize)
        {
            substring = substring.ToLower();
        }
        
        return collection.FirstOrDefault(s => s.Contains(substring));
    }

    /// <summary>
    /// If the collection contains a term which substrings a string.
    /// </summary>
    /// <param name="substring">The string to see if any term substrings it.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which substrings the string if one is found, null otherwise.</returns>
    public string? CollectionSubstring(string substring, bool normalize = true)
    {
        return CollectionSubstring(_collection, substring, normalize);
    }

    /// <summary>
    /// If a collection contains a term which a string starts with.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="str">The string to see if it starts with any term.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which the string starts with if one is found, null otherwise.</returns>
    public static string? StringStartsWith(ICollection<string> collection, string str, bool normalize = true)
    {
        if (normalize)
        {
            str = str.ToLower();
        }

        return collection.FirstOrDefault(s => str.StartsWith(s));
    }

    /// <summary>
    /// If the collection contains a term which a string starts with.
    /// </summary>
    /// <param name="str">The string to see if it starts with any term.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which the string starts with if one is found, null otherwise.</returns>
    public string? StringStartsWith(string str, bool normalize = true)
    {
        return StringStartsWith(_collection, str, normalize);
    }

    /// <summary>
    /// If a collection contains a term which a string ends with.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="str">The string to see if it ends with any term.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which the string ends with if one is found, null otherwise.</returns>
    public static string? StringEndsWith(ICollection<string> collection, string str, bool normalize = true)
    {
        if (normalize)
        {
            str = str.ToLower();
        }

        return collection.FirstOrDefault(s => str.EndsWith(s));
    }

    /// <summary>
    /// If the collection contains a term which a string starts with.
    /// </summary>
    /// <param name="str">The string to see if it starts with any term.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which the string starts with if one is found, null otherwise.</returns>
    public string? StringEndsWith(string str, bool normalize = true)
    {
        return StringEndsWith(_collection, str, normalize);
    }

    /// <summary>
    /// If a collection contains a term which a string substrings.
    /// </summary>
    /// <param name="collection">The existing collection.</param>
    /// <param name="str">The string to see if it substrings any term.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which the string substrings if one is found, null otherwise.</returns>
    public static string? StringContains(ICollection<string> collection, string str, bool normalize = true)
    {
        if (normalize)
        {
            str = str.ToLower();
        }

        return collection.FirstOrDefault(s => str.Contains(s));
    }

    /// <summary>
    /// If the collection contains a term which a string substrings.
    /// </summary>
    /// <param name="str">The string to see if it substrings any term.</param>
    /// <param name="normalize">If the contents should be normalized.</param>
    /// <returns>The first term which the string substrings if one is found, null otherwise.</returns>
    public string? StringContains(string str, bool normalize = true)
    {
        return StringContains(_collection, str, normalize);
    }
}