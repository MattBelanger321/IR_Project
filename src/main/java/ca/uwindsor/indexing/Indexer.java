package ca.uwindsor.indexing;

import java.io.BufferedReader;
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

import org.apache.lucene.document.Document;
import org.apache.lucene.document.Field;
import org.apache.lucene.document.StringField;
import org.apache.lucene.document.TextField;
import org.apache.lucene.index.IndexWriter;
import org.apache.lucene.index.IndexWriterConfig;
import org.apache.lucene.store.FSDirectory;

import ca.uwindsor.common.Constants;

/**
 * Index all text files under a directory.
 * Based on code by Jianguo Lu.
 */
public class Indexer {
	/**
	 * Count how many files have been indexed.
	 */
	static int counter = 0;

	/**
	 * Run the indexing.
	 * 
	 * @param args Nothing.
	 * @throws IOException If a file reading error occurs during execution.
	 */
	public static void main(String[] args) throws IOException {
		// Writer for our indexing.
		IndexWriter writer = new IndexWriter(
				// The root path for the directory to index.
				FSDirectory.open(Paths.get(Constants.dataIndex)),
				// Use our custom analyzer.
				new IndexWriterConfig(new ComputerScienceAnalyzer()));

		// Loop over all files to index.
		Files.walkFileTree(Paths.get(Constants.data), new SimpleFileVisitor<Path>() {
			// Visit the file and index it.
			public FileVisitResult visitFile(Path file, BasicFileAttributes attrs) throws IOException {
				indexDoc(writer, file);
				return FileVisitResult.CONTINUE;
			}
		});

		// Cleanup by closing the index writer.
		writer.close();
	}

	/**
	 * Indexes a single document.
	 * 
	 * @param writer The index to write to.
	 * @param file   The file to index.
	 * @throws IOException If the file cannot be read for indexing.
	 */
	static void indexDoc(IndexWriter writer, Path file) throws IOException {
		// Open the readers for the file.
		InputStream stream = Files.newInputStream(file);
		InputStreamReader inputStreamReader = new InputStreamReader(stream, StandardCharsets.UTF_8);
		BufferedReader br = new BufferedReader(inputStreamReader);

		// Get the title which should be indexed.
		String title = br.readLine();

		// Let us store the initial details of the file.
		// This should capture author names for instance.
		StringBuilder details = new StringBuilder();
		for (int i = 0; i < Constants.indexedLines; i++) {
			String line = br.readLine();
			// We do not care for empty lines.
			if (!line.isEmpty()) {
				if (details.length() > 0) {
					details.append(" ");
				}
				details.append(line);
			}
		}

		// Build the indexed Lucene document.
		Document doc = new Document();
		doc.add(new StringField("path", file.toString(), Field.Store.YES));
		doc.add(new StringField("title", title, Field.Store.YES));
		doc.add(new StringField("details", details.toString(), Field.Store.YES));
		doc.add(new TextField("contents", br));

		// Index the document.
		writer.addDocument(doc);

		// Track some output.
		counter++;
		if (counter % 1000 == 0) {
			System.out.println("Indexing file " + counter + ": " + file.getFileName());
		}

		// Close the readers for the file.
		br.close();
		inputStreamReader.close();
		stream.close();
	}
}