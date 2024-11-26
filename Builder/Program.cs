using SearchEngine.Server;

// Default values.
int totalResults = 5000000;
int min = 5;
int max = 5;
bool mitigate = true;
bool cluster = true;
bool rank = true;
bool reset = true;
string? startingCategory = null;
string? startingOrder = null;
string? startingBy = null;

// Try and get how many results we want to download.
if (args.Length > 0 && int.TryParse(args[0], out int temp))
{
    totalResults = Math.Max(temp, 1);
}

// Try and get the minimum number of clusters to perform.
if (args.Length > 1 && int.TryParse(args[1], out temp))
{
    min = Math.Max(temp, 2);
}

// Try and get the maximum number of clusters to perform.
if (args.Length > 2 && int.TryParse(args[2], out temp))
{
    max = Math.Max(temp, min);
}

// Try and get if we should perform mitigation.
if (args.Length > 3)
{
    _ = bool.TryParse(args[3], out mitigate);
}

// Try and get if we should perform clustering.
if (args.Length > 4)
{
    _ = bool.TryParse(args[4], out cluster);
}

// Try and get if we should perform PageRank.
if (args.Length > 4)
{
    _ = bool.TryParse(args[4], out rank);
}

// Try and get if we should reset the entire database.
if (args.Length > 3)
{
    _ = bool.TryParse(args[4], out reset);
}

// Try and get the category we should start searching from.
if (args.Length > 4)
{
    startingCategory = args[4];
}

// Try and get the ordering mode for searching.
if (args.Length > 5)
{
    startingOrder = args[5];
}

// Try and get the sorting by mode for searching.
if (args.Length > 6)
{
    startingBy = args[6];
}

// Run everything from scraping to indexing.
await ScrappingService.Scrape(totalResults:totalResults, min:min, max:max, mitigate:mitigate, cluster:cluster, rank:rank, reset:reset, startingCategory:startingCategory, startingOrder:startingOrder, startingBy:startingBy);
