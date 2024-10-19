namespace SearchEngine.Lucene.Utility;

public readonly struct TermMappings : IComparable<TermMappings>
{
    public readonly string KeyTerm;
    
    public readonly SortedSet<string> Abbreviations;

    public TermMappings(string keyTerm, SortedSet<string> abbreviations)
    {
        KeyTerm = keyTerm;
        Abbreviations = abbreviations;
    }

    public int CompareTo(TermMappings other)
    {
        return string.Compare(KeyTerm, other.KeyTerm, StringComparison.OrdinalIgnoreCase);
    }
}