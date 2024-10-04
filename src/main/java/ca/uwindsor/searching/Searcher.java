package ca.uwindsor.searching;

import java.io.BufferedWriter;
import java.io.File;
import java.io.FileWriter;
import java.io.IOException;
import java.nio.file.Paths;

import ca.uwindsor.analyzing.ComputerScienceAnalyzer;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.lucene.document.Document;
import org.apache.lucene.index.DirectoryReader;
import org.apache.lucene.index.IndexReader;
import org.apache.lucene.index.Terms;
import org.apache.lucene.index.TermsEnum;
import org.apache.lucene.queryparser.classic.ParseException;
import org.apache.lucene.queryparser.classic.QueryParser;
import org.apache.lucene.search.BooleanClause;
import org.apache.lucene.search.BooleanQuery;
import org.apache.lucene.search.IndexSearcher;
import org.apache.lucene.search.Query;
import org.apache.lucene.search.TopDocs;
import org.apache.lucene.store.FSDirectory;
import org.apache.lucene.util.BytesRef;

import ca.uwindsor.common.Constants;

/**
 * Simplified Lucene Search.
 * Based on code by Jianguo Lu.
 */
public class Searcher
{
	// the logger used for this class
	private static final Logger logger = LogManager.getLogger(Searcher.class);

	/**
	 * Run some searching.
	 * @param args For now, these are not used and the request is hardcoded.
	 * @throws Exception Error reading from the index.
	 */
	public static void main(String[] args) throws Exception
	{
		// For now, our test strings are hardcoded.
		String request = "artificial intelligence and machine learning concepts";

		// Load the indexed files.
		IndexReader reader = DirectoryReader.open(FSDirectory.open(Paths.get(Constants.DATA_INDEX)));
		IndexSearcher searcher = new IndexSearcher(reader);

		// Get the top five results.
		TopDocs results = CombinedSearch(searcher, request, 5);

		// Helper to check the stemmed files.
		ComputerScienceAnalyzer stemmedChecker = new ComputerScienceAnalyzer(false);

		// Ensure the folder to save stemmed results exists.
		String stemsFolder = "Stems";
		File directory = new File(stemsFolder);
		if (!directory.exists())
		{
			if (!directory.mkdir())
			{
				logger.error("Failed to create stems directory.");
			}
		}

		// Output details of the files we returned.
		logger.info(results.totalHits + " total matching documents.");
		for (int i = 0; i < 5 && i < results.scoreDocs.length; i++)
		{
			// We should look into other methods to avoid deprecation, but everything I read say to use this...
			@SuppressWarnings("deprecation")
			Document doc = searcher.doc(results.scoreDocs[i].doc);

			// Write the path to the file.
			logger.info("File " + (i + 1) + " - " + doc.get("path"));

			// Write the title.
			String title = doc.get("title");
			if (title != null)
			{
				logger.info("Title: " + doc.get("title"));
			}

			// Get how many times keywords are in a document for seeing how the matching is working.
			@SuppressWarnings("deprecation")
			Terms terms = reader.getTermVector(results.scoreDocs[i].doc, Constants.FieldNames.KEYWORDS.getValue());
			if (terms != null)
			{
				// Access the terms for this field.
				TermsEnum termsEnum = terms.iterator();

				// Iterate through all terms.
				BytesRef term;
				while ((term = termsEnum.next()) != null)
				{
					// Get the frequency of the term in the document.
					logger.info(term.utf8ToString() + " = " + (int) termsEnum.totalTermFreq());
				}
			}

			// Get the document.
			String stemmedContents = stemmedChecker.analyzeText(Constants.FieldNames.STEMMED_CONTENTS.getValue(), doc.get(Constants.FieldNames.STEMMED_CONTENTS.getValue()));

			// Write the "stemmed_contents" field to the specified file.
			if (stemmedContents != null && !stemmedContents.isEmpty())
			{
				String output = "Stemmed " + (i + 1) + ".txt";

				try (BufferedWriter writer = new BufferedWriter(new FileWriter(stemsFolder + "/" + output)))
				{
					writer.write(stemmedContents);
				}
				logger.info("Stemmed contents written to file " + output);
			}
			else if (stemmedContents == null)
			{
				logger.error("Stemmed contents string is null.");
			}
			else
            {
                logger.error("Stemmed contents string is empty.");
            }
		}

		// Cleanup the reader.
		reader.close();
	}

	/**
	 * Run a combined search using both the contents and the keywords.
	 * @param searcher The searcher.
	 * @param request The request we want to search for.
	 * @param number The number of results we want.
	 * @return The top results matching the request.
	 * @throws IOException Error reading the indexed documents.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static TopDocs CombinedSearch(IndexSearcher searcher, String request, int number) throws IOException, ParseException
    {
		// Add all searches, treating them similar to a logical "OR".
		BooleanQuery.Builder builder = new BooleanQuery.Builder();
		builder.add(TitleQuery(request), BooleanClause.Occur.SHOULD);
		builder.add(ContentsQuery(request, true), BooleanClause.Occur.SHOULD);
		builder.add(ContentsQuery(request, false), BooleanClause.Occur.SHOULD);
		builder.add(KeywordsQuery(request), BooleanClause.Occur.SHOULD);
		return searcher.search(builder.build(), number);
	}

	/**
	 * Run a search using the title.
	 * @param searcher The searcher.
	 * @param request The request we want to search for.
	 * @param number The number of results we want.
	 * @return The top results matching the request.
	 * @throws IOException Error reading the indexed documents.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static TopDocs TitleSearch(IndexSearcher searcher, String request, int number) throws IOException, ParseException
	{
		return searcher.search(TitleQuery(request), number);
	}

	/**
	 * Run a search using the contents.
	 * @param searcher The searcher.
	 * @param request The request we want to search for.
	 * @param number The number of results we want.
	 * @param stemmed If the stemmed contents should be searched.
	 * @return The top results matching the request.
	 * @throws IOException Error reading the indexed documents.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static TopDocs ContentsSearch(IndexSearcher searcher, String request, int number, Boolean stemmed) throws IOException, ParseException
    {
		return searcher.search(ContentsQuery(request, stemmed), number);
	}

	/**
	 * Run a search using the keywords.
	 * @param searcher The searcher.
	 * @param request The request we want to search for.
	 * @param number The number of results we want.
	 * @return The top results matching the request.
	 * @throws IOException Error reading the indexed documents.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static TopDocs KeywordsSearch(IndexSearcher searcher, String request, int number) throws IOException, ParseException
	{
		return searcher.search(KeywordsQuery(request), number);
	}

	/**
	 * Create a query for the title.
	 * @param request The request we want to search for.
	 * @return The query.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static Query TitleQuery(String request) throws ParseException
	{
		return new QueryParser(Constants.FieldNames.TITLE.getValue(), new ComputerScienceAnalyzer(false)).parse(request);
	}

	/**
	 * Create a query for the contents.
	 * @param request The request we want to search for.
	 * @param stemmed If the stemmed contents should be searched.
	 * @return The query.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static Query ContentsQuery(String request, Boolean stemmed) throws ParseException
    {
		return new QueryParser(stemmed ? Constants.FieldNames.STEMMED_CONTENTS.getValue() : Constants.FieldNames.CONTENTS.getValue(), new ComputerScienceAnalyzer(false)).parse(request);
	}

	/**
	 * Create a query for the keywords.
	 * @param request The request we want to search for.
	 * @return The query.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static Query KeywordsQuery(String request) throws ParseException
	{
		return new QueryParser(Constants.FieldNames.KEYWORDS.getValue(), new ComputerScienceAnalyzer(true)).parse(request);
	}
}