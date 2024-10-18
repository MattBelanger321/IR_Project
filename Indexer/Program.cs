using Indexer;
using SearchEngine.Lucene;

// Get all the documents to build our dataset.
await ArXiv.SaveDocumentsGetLinksAsync();

// Index the dataset.
Core.Index();