using SearchEngine.Server;

// Get all the documents to build our dataset, creating a fresh database for them.
//await ScrappingService.Scrape(totalResults:2000, reset:true);
await MitigatedInformation.Perform();