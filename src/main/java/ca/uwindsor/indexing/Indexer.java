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
import java.util.HashMap;
import java.util.HashSet;
import java.util.Map;

import org.apache.lucene.document.*;
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
	 * Store the computer science terms.
	 */
	private static HashSet<String> terms;

	/**
	 * Run the indexing.
	 * 
	 * @param args Nothing.
	 * @throws IOException If a file reading error occurs during execution.
	 */
	public static void main(String[] args) throws IOException
	{
		// Load the terms.
		terms = Constants.getTerms();

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
	static void indexDoc(IndexWriter writer, Path file) throws IOException
	{
		// Open the readers for the file.
		InputStream stream = Files.newInputStream(file);
		InputStreamReader inputStreamReader = new InputStreamReader(stream, StandardCharsets.UTF_8);
		BufferedReader br = new BufferedReader(inputStreamReader);

		// We will index the title.
		String title = null;

		// Let us store the initial details of the file.
		// This should capture author names for instance.
		StringBuilder details = new StringBuilder();

		// Store the entire text of the document for processing later.
		StringBuilder text = new StringBuilder();

		// How many lines have been counted so far for the initial indexing.
		int lineCount = 0;

		// Read the entire file.
		String line;
		while ((line = br.readLine()) != null)
		{
			// Get the title which should be indexed.
			if (title == null)
			{
				title = line;
			}

			// We do not care for empty lines.
			if (!line.isEmpty())
			{
				// Check if this line should be fully indexed.
				if (lineCount < Constants.indexedLines)
				{
					// Ensure we are not starting with an empty space.
					if (details.length() > 0)
					{
						details.append(" ");
					}
					details.append(line);
				}

				// Store the entire, regular text.
				if (text.length() > 0)
				{
					text.append(" ");
				}
				text.append(line);
			}

			lineCount++;
		}

		// Build the indexed Lucene document.
		Document doc = new Document();
		doc.add(new StringField("path", file.toString(), Field.Store.YES));
		doc.add(new StringField("title", title == null ? "" : title, Field.Store.YES));
		doc.add(new StringField("details", details.toString(), Field.Store.YES));
		doc.add(new TextField("contents", br));

		// Store the terms that have been found throughout the entire text.
		for (Map.Entry<String, Integer> entry : GetTermFrequency(text).entrySet())
		{
			doc.add(new TextField("term", entry.getKey(), Field.Store.YES));
			doc.add(new IntField("frequency", entry.getValue(), Field.Store.YES));
		}

		// Index the document.
		writer.addDocument(doc);

		// Close the readers for the file.
		br.close();
		inputStreamReader.close();
		stream.close();

		// Track some output.
		counter++;
		if (counter % 1000 == 0)
		{
			System.out.println("Indexing file " + counter + ": " + file.getFileName());
		}
	}

	/**
	 * Get how frequent terms occur.
	 * @param text The text read from a file.
	 * @return The HashMap of the terms and their frequency.
	 */
	private static Map<String, Integer> GetTermFrequency(StringBuilder text)
	{
		// Convert the text to a string.
		String content = text.toString();
		for (String term : terms)
		{
			// For the terms we want to ensure are not split, replace the space with an underscore.
			String modifiedTerm = term.replace(' ', '_');
			content = content.replaceAll("\\b" + term + "\\b", modifiedTerm);
		}

		// Get the found terms in the text.
		Map<String, Integer> foundTerms = new HashMap<>();

		// Ensure we split every word, but not the "_" we have added.
		for (String word : content.split("(?<!\\S)\\s+"))
		{
			// Convert the placeholders back to the actual terms.
			String term = word.replace('_', ' ').toLowerCase();
			if (terms.contains(term))
			{
				foundTerms.put(term, foundTerms.getOrDefault(term, 0) + 1);
			}
		}

		return foundTerms;
	}
}