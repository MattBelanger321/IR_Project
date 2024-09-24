package ca.uwindsor.indexing;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.FileVisitResult;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.nio.file.SimpleFileVisitor;
import java.nio.file.attribute.BasicFileAttributes;
import java.util.HashMap;
import java.util.Map;

import ca.uwindsor.analyzing.ComputerScienceAnalyzer;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.miscellaneous.PerFieldAnalyzerWrapper;
import org.apache.lucene.document.*;
import org.apache.lucene.index.IndexOptions;
import org.apache.lucene.index.IndexWriter;
import org.apache.lucene.index.IndexWriterConfig;
import org.apache.lucene.store.FSDirectory;

import ca.uwindsor.common.Constants;

/**
 * Index all text files under a directory.
 * Based on code by Jianguo Lu.
 */
public class Indexer
{
	/**
	 * Count how many files have been indexed.
	 */
	private static int counter = 0;

	/**
	 * Custom field used for the keywords only section.
	 */
	private static FieldType keywordsField;

	/**
	 * Run the indexing.
	 * @param args Nothing.
	 * @throws IOException If a file reading error occurs during execution.
	 */
	public static void main(String[] args) throws IOException
	{
		// Define our custom field to store the frequency of terms.
		keywordsField = new FieldType(TextField.TYPE_STORED);
		keywordsField.setStoreTermVectors(true);
		keywordsField.setStoreTermVectorPositions(true);
		keywordsField.setStoreTermVectorOffsets(true);
		keywordsField.setTokenized(true);
		keywordsField.setIndexOptions(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);

		// Create the override analyzer for the content field to only match computer science terms.
		Map<String, Analyzer> overrides = new HashMap<>();
		overrides.put(Constants.FieldKeywords, new ComputerScienceAnalyzer(true));

		// Writer for our indexing.
		IndexWriter writer = new IndexWriter(
				// The root path for the directory to index.
				FSDirectory.open(Paths.get(Constants.dataIndex)),
				// Set the default analyzer to match everything and override for the keywords.
				new IndexWriterConfig(new PerFieldAnalyzerWrapper(new ComputerScienceAnalyzer(false), overrides)));

		// Loop over all files to index.
		Files.walkFileTree(Paths.get(Constants.data), new SimpleFileVisitor<Path>()
		{
			// Visit the file and index it.
			public FileVisitResult visitFile(Path file, BasicFileAttributes attrs) throws IOException
			{
				indexDoc(writer, file);
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
		StringBuilder contents = new StringBuilder();
		while ((line = reader.readLine()) != null)
		{
			contents.append(line).append(System.lineSeparator());
		}

		// Build the indexed Lucene document.
		Document doc = new Document();

		// The path and title are stored as entire strings.
		doc.add(new StringField(Constants.FieldPath, file.toString(), Field.Store.YES));
		doc.add(new StringField(Constants.FieldTitle, title == null ? "" : title, Field.Store.YES));

		// The contents are tokenized normally.
		doc.add(new TextField(Constants.FieldContents, contents.length() == 0 ? "" : contents.toString(), Field.Store.NO));

		// The keywords are stored noting their frequency.
		doc.add(new Field(Constants.FieldKeywords, contents.length() == 0 ? "" : contents.toString(), keywordsField));

		// Index the document.
		writer.addDocument(doc);

		// Cleanup the readers.
		reader.close();
		inputStreamReader.close();
		stream.close();

		// Track some output.
		if (++counter % 1000 == 0)
		{
			System.out.println("Indexing file " + counter);
		}
	}
}