package ca.uwindsor.searching;

import java.io.IOException;
import java.nio.file.Paths;

import ca.uwindsor.analyzing.KeyTermsAnalyzer;
import org.apache.lucene.analysis.standard.StandardAnalyzer;
import org.apache.lucene.document.Document;
import org.apache.lucene.index.*;
import org.apache.lucene.queryparser.classic.ParseException;
import org.apache.lucene.queryparser.classic.QueryParser;
import org.apache.lucene.search.*;
import org.apache.lucene.store.FSDirectory;

import ca.uwindsor.common.Constants;
import org.apache.lucene.util.BytesRef;

/**
 * Simplified Lucene Search.
 * Based on code by Jianguo Lu.
 */
public class Searcher
{
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
		// Swap the method for which way we want to search.
		// The options are "ContentsSearch", "KeywordsSearch", or "CombinedSearch".
		// Eventually, we can try doing a method which uses one metric and then another as a fallback.
		// For instance, maybe use keywords only to start, and if nothing/little returns we can do the rest standard.
		TopDocs results = KeywordsSearch(searcher, request, 5);

		// Output details of the files we returned.
		System.out.println(results.totalHits + " total matching documents");
		for (int i = 0; i < 5 && i < results.scoreDocs.length; i++)
		{
			// We should look into other methods to avoid deprecation, but everything I read say to use this...
			@SuppressWarnings("deprecation")
			Document doc = searcher.doc(results.scoreDocs[i].doc);

			// Write the path to the file.
			System.out.println((i + 1) + ". " + doc.get("path"));

			// Write the title.
			String title = doc.get("title");
			if (title != null)
			{
				System.out.println("   Title: " + doc.get("title"));
			}

			// Get how many times keywords are in a document for seeing how the matching is working.
			@SuppressWarnings("deprecation")
			Terms terms = reader.getTermVector(results.scoreDocs[i].doc, Constants.FIELD_KEYWORDS);
			if (terms != null)
			{
				// Access the terms for this field.
				TermsEnum termsEnum = terms.iterator();

				// Iterate through all terms.
				BytesRef term;
				while ((term = termsEnum.next()) != null)
				{
					// Get the frequency of the term in the document.
					System.out.println("   " + term.utf8ToString() + " = " + (int) termsEnum.totalTermFreq());
				}
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
		// Add both searches, treating them similar to a logical "OR".
		BooleanQuery.Builder builder = new BooleanQuery.Builder();
		builder.add(ContentsQuery(request), BooleanClause.Occur.SHOULD);
		builder.add(KeywordsQuery(request), BooleanClause.Occur.SHOULD);
		return searcher.search(builder.build(), number);
	}

	/**
	 * Run a search using the contents.
	 * @param searcher The searcher.
	 * @param request The request we want to search for.
	 * @param number The number of results we want.
	 * @return The top results matching the request.
	 * @throws IOException Error reading the indexed documents.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static TopDocs ContentsSearch(IndexSearcher searcher, String request, int number) throws IOException, ParseException
    {
		return searcher.search(ContentsQuery(request), number);
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
	 * Create a query for the contents.
	 * @param request The request we want to search for.
	 * @return The query.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static Query ContentsQuery(String request) throws ParseException
    {
		return new QueryParser(Constants.FIELD_CONTENTS, new StandardAnalyzer()).parse(request);
	}

	/**
	 * Create a query for the keywords.
	 * @param request The request we want to search for.
	 * @return The query.
	 * @throws ParseException Error parsing the requested search.
	 */
	private static Query KeywordsQuery(String request) throws ParseException
	{
		return new QueryParser(Constants.FIELD_KEYWORDS, new KeyTermsAnalyzer()).parse(request);
	}
}