using SearchEngine.Server;

// Get all the documents to build our dataset, creating a fresh database for them.
//await ScrappingService.Scrape(10000, 1000, true, process:false, summarize:false, index: false);
Dictionary<string, double> result = await PageRank.Perform();
foreach (KeyValuePair<string, double> kvp in result)
{
    Console.WriteLine($"{kvp.Key} = {kvp.Value}");
}