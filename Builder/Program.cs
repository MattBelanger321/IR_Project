using SearchEngine.Server;

// Get all the documents to build our dataset, creating a fresh database for them.
//await ScrappingService.Scrape(totalResults:5000000, max:5, reset:true);
await ArXiv.Scrape(5000000);