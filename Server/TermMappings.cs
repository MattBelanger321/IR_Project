namespace SearchEngine.Server;

/// <summary>
/// Handle mappings between a key term and its abbreviations.
/// </summary>
public readonly struct TermMappings : IComparable<TermMappings>
{
    /// <summary>
    /// The key term.
    /// </summary>
    public readonly string KeyTerm;
    
    /// <summary>
    /// The abbreviations.
    /// </summary>
    public readonly ICollection<string> Abbreviations;

    /// <summary>
    /// Define the key term and its mappings.
    /// </summary>
    /// <param name="keyTerm">The key term.</param>
    /// <param name="abbreviations">The abbreviations.</param>
    public TermMappings(string keyTerm, SortedSet<string> abbreviations)
    {
        KeyTerm = keyTerm;
        Abbreviations = abbreviations;
    }

    /// <summary>
    /// Compare these by comparing their key terms.
    /// </summary>
    /// <param name="other">The other term mapping object.</param>
    /// <returns>Which string is greater.</returns>
    public int CompareTo(TermMappings other)
    {
        return string.Compare(KeyTerm, other.KeyTerm, StringComparison.OrdinalIgnoreCase);
    }
}