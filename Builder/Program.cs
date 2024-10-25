using Builder;
using SearchEngine.Server;

// Get all the documents to build our dataset.
await ArXiv.SaveDocumentsGetLinksAsync();

// Summarize documents.
await Ollama.Summarize();

// Process all documents.
await Embeddings.Preprocess();