using Indexer;
using SearchEngine.Lucene;

// Get all the documents to build our dataset.
//await ArXiv.SaveDocumentsGetLinksAsync();

// Try to summarize documents.
if (await Ollama.Summarize())
{
    // Index the dataset.
    Core.Index();
}