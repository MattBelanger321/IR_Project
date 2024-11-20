using SearchEngine.Server;

// Get all the documents to build our dataset, creating a fresh database for them.
await ScrappingService.Scrape(2000, 2000, reset:true, process:true, summarize:true, index: true);