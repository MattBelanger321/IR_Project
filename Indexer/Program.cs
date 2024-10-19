using Indexer;
using SearchEngine.Lucene;

// Get all the documents to build our dataset.
// Comment this out if you don't want to look for more data.
await ArXiv.SaveDocumentsGetLinksAsync();

// Index the dataset.
Core.Index();