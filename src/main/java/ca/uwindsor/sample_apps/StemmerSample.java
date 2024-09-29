package ca.uwindsor.sample_apps;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.FileWriter;
import java.io.IOException;
import java.io.InputStream;
import java.io.InputStreamReader;
import java.nio.charset.StandardCharsets;
import java.nio.file.FileVisitResult;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.SimpleFileVisitor;
import java.nio.file.attribute.BasicFileAttributes;
import java.util.HashMap;
import java.util.Map;

import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.miscellaneous.PerFieldAnalyzerWrapper;
import org.apache.lucene.analysis.standard.StandardAnalyzer;
import org.apache.lucene.document.Document;
import org.apache.lucene.document.Field;
import org.apache.lucene.document.FieldType;
import org.apache.lucene.document.StringField;
import org.apache.lucene.document.TextField;
import org.apache.lucene.index.DirectoryReader;
import org.apache.lucene.index.IndexOptions;
import org.apache.lucene.index.IndexWriter;
import org.apache.lucene.index.IndexWriterConfig;
import org.apache.lucene.queryparser.classic.ParseException;
import org.apache.lucene.queryparser.classic.QueryParser;
import org.apache.lucene.search.IndexSearcher;
import org.apache.lucene.search.Query;
import org.apache.lucene.search.ScoreDoc;
import org.apache.lucene.search.TopDocs;
import org.apache.lucene.store.FSDirectory;

import ca.uwindsor.analyzing.ComputerScienceAnalyzer;
import ca.uwindsor.common.Constants;


public class StemmerSample {
	
	// the logger used for this class
	private static final Logger logger = LogManager.getLogger(StemmerSample.class);
	/**
	 * Count how many files have been indexed.
	 */
	private static int counter = 0;

	/**
	 * Custom field used for the keywords only section.
	 */
	private static FieldType counterField;

	private static final String INDEX_PATH = "stemmer_sample_index";

	/**
	 * This application indexs the data using the stemmer for debugging purposes
	 * @param args Nothing.
	 * @throws IOException If a file reading error occurs during execution.
	 */
	public static void main(String[] args) throws IOException
	{
		// Define our custom field to store the frequency of terms.
		counterField = new FieldType(TextField.TYPE_STORED);
		counterField.setStoreTermVectors(true);
		counterField.setStoreTermVectorPositions(true);
		counterField.setStoreTermVectorOffsets(true);
		counterField.setTokenized(true);
		counterField.setIndexOptions(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);

		indexStems();
		writeStemmedDocument("stemmed.txt");	// print the stemmed doucment to a file
	}


	// this functions indexs the dataset using only the stemmer for debugging purposes
	public static void indexStems() throws IOException{
		// Create the override analyzer for the content field to only match computer science terms.
		Map<String, Analyzer> overrides = new HashMap<>();
		overrides.put(Constants.FieldNames.STEMMED_CONTENTS.getValue(), new ComputerScienceAnalyzer());

		// Writer for our indexing.
		IndexWriter writer = new IndexWriter(
				// The root path for the directory to index.
				FSDirectory.open(Paths.get(INDEX_PATH)),
				// Set the default analyzer to match everything and override for the keywords.
				new IndexWriterConfig(new PerFieldAnalyzerWrapper(new StandardAnalyzer(), overrides)));

		// Loop over all files to index.
		Files.walkFileTree(Paths.get(Constants.DATA), new SimpleFileVisitor<Path>()
		{
			// Visit the file and index it.
			@Override
			public FileVisitResult visitFile(Path file, BasicFileAttributes attrs) throws IOException
			{
				// Track some output.
				if (++counter % 1000 == 0)
				{
					logger.debug("Indexing file #" + counter + ": " + file.toString());
				}else{
					logger.trace("Indexing file #" + counter + ": " + file.toString());
				}

				indexDoc(writer, file);

				// Track some output.
				if (counter % 1000 == 0)
				{
					logger.info("Indexed file #" + counter + ": " + file.toString());
				}else{
					logger.trace("Indexed file #" + counter + ": " + file.toString());
				}
				return FileVisitResult.CONTINUE;
			}
		});

		// Cleanup by closing the index writer.
		writer.close();
	}

	/**
	 * Indexes a single document.
	 * @param writer The index to write to.
	 * @param file   The file to index.
	 * @throws IOException If the file cannot be read for indexing.
	 */
	static void indexDoc(IndexWriter writer, Path file) throws IOException
	{
		// Read the file.
		InputStream stream = Files.newInputStream(file);
		InputStreamReader inputStreamReader = new InputStreamReader(stream, StandardCharsets.UTF_8);
		BufferedReader reader = new BufferedReader(inputStreamReader);

		// The title is the first line.
		String title = reader.readLine();

		// Read all lines and also start capturing the computer science terms.
		String line;
		StringBuilder contents = new StringBuilder(title);
		contents.append(System.lineSeparator());
		logger.trace("Reading file...");
		while ((line = reader.readLine()) != null)
		{
			contents.append(line).append(System.lineSeparator());
		}
		logger.trace("Read file.");

		// Build the indexed Lucene document.
		Document doc = new Document();
		logger.trace("Adding Fields to Document...");
		if (contents.length() > 0)
		{
			doc.add(new StringField(Constants.FieldNames.TITLE.getValue(), title, Field.Store.YES));
			// The contents are tokenized normally.
			doc.add(new TextField(Constants.FieldNames.STEMMED_CONTENTS.getValue(), contents.toString(), Field.Store.NO));
		}
		logger.trace("Added Fields");

		// Index the document.
		logger.trace("Adding Document to Index...");
		writer.addDocument(doc);
		logger.trace("Added Document");

		// Cleanup the readers.
		reader.close();
		inputStreamReader.close();
		stream.close();
	}

    @SuppressWarnings("deprecation")
	private static void writeStemmedDocument(String stemmedtxt) {
        try {
            // Open the index
            FSDirectory directory = FSDirectory.open(Paths.get(INDEX_PATH));
            DirectoryReader reader = DirectoryReader.open(directory);
            IndexSearcher searcher = new IndexSearcher(reader);

            // Build a query to find the document by title
            QueryParser parser = new QueryParser(Constants.FieldNames.TITLE.getValue(), new StandardAnalyzer());
            Query query = parser.parse("Collaborative Decision-making Processes for");

            // Search for the document
            TopDocs results = searcher.search(query, 1); // Limiting to 1 result
            ScoreDoc[] hits = results.scoreDocs;

            if (hits.length > 0) {
                // Get the document
                Document doc = searcher.doc(hits[0].doc);
                String stemmedContents = doc.get(Constants.FieldNames.STEMMED_CONTENTS.getValue());

                // Write the "stemmed_contents" field to the specified file
                try (BufferedWriter writer = new BufferedWriter(new FileWriter(stemmedtxt))) {
                    writer.write(stemmedContents);
                }

                System.out.println("Stemmed contents written to file: " + stemmedtxt);
            } else {
                System.out.println("Document not found.");
            }

            // Close the reader
            reader.close();
        } catch (IOException | ParseException e) {
			System.out.println(e);
        }
    }
}
