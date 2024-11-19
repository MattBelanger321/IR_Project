using SearchEngine.Server;

// Get all the documents to build our dataset, creating a fresh database for them.
await ScrappingService.Scrape(10, 10, true, process:false, summarize:false, index: false);