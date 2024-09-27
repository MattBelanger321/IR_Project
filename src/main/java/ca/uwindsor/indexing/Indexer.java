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

import ca.uwindsor.analyzing.KeyTermsAnalyzer;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.miscellaneous.PerFieldAnalyzerWrapper;
import org.apache.lucene.analysis.standard.StandardAnalyzer;
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
	private static FieldType counterField;

	/**
	 * Run the indexing.
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

		// Create the override analyzer for the content field to only match computer science terms.
		Map<String, Analyzer> overrides = new HashMap<>();
		overrides.put(Constants.FIELD_KEYWORDS, new KeyTermsAnalyzer());

		// Writer for our indexing.
		IndexWriter writer = new IndexWriter(
				// The root path for the directory to index.
				FSDirectory.open(Paths.get(Constants.DATA_INDEX)),
				// Set the default analyzer to match everything and override for the keywords.
				new IndexWriterConfig(new PerFieldAnalyzerWrapper(new StandardAnalyzer(), overrides)));

		// Loop over all files to index.
		Files.walkFileTree(Paths.get(Constants.DATA), new SimpleFileVisitor<Path>()
		{
			// Visit the file and index it.
            @Override
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
		StringBuilder contents = new StringBuilder(title);
		while ((line = reader.readLine()) != null)
		{
			contents.append(line).append(System.lineSeparator());
		}

		// Build the indexed Lucene document.
		Document doc = new Document();

		// The path and title are stored as entire strings.
		doc.add(new StringField(Constants.FIELD_PATH, file.toString(), Field.Store.YES));
		doc.add(new StringField(Constants.FIELD_TITLE, title, Field.Store.YES));

		if (contents.length() > 0)
		{
			// The contents are tokenized normally.
			doc.add(new TextField(Constants.FIELD_CONTENTS, contents.toString(), Field.Store.NO));

			// The keywords are stored noting their frequency.
			doc.add(new Field(Constants.FIELD_KEYWORDS, contents.toString(), counterField));
		}

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