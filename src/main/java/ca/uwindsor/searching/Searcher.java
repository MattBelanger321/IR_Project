package ca.uwindsor.searching;

import java.nio.file.Paths;

import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.standard.StandardAnalyzer;
import org.apache.lucene.document.Document;
import org.apache.lucene.index.DirectoryReader;
import org.apache.lucene.index.IndexReader;
import org.apache.lucene.queryparser.classic.QueryParser;
import org.apache.lucene.search.IndexSearcher;
import org.apache.lucene.search.Query;
import org.apache.lucene.search.TopDocs;
import org.apache.lucene.store.FSDirectory;

import ca.uwindsor.common.Constants;
import ca.uwindsor.indexing.ComputerScienceAnalyzer;

/**
 * Simplified Lucene Search .
 * Based on code by Jianguo Lu.
 */
public class Searcher {
	public static void main(String[] args) throws Exception {
		// Load the indexed files.
		IndexReader reader = DirectoryReader.open(FSDirectory.open(Paths.get(Constants.dataIndex)));
		IndexSearcher searcher = new IndexSearcher(reader);
		// Use our custom analyzer.
		QueryParser parser = new QueryParser("contents", new ComputerScienceAnalyzer());
		// For now, this is a hardcoded query.
		Query query = parser.parse("information OR retrieval");
		// Get the top five results.
		TopDocs results = searcher.search(query, 5);
		System.out.println(results.totalHits + " total matching documents");
		for (int i = 0; i < 5 && i < results.scoreDocs.length; i++) {
			@SuppressWarnings("deprecation")
			Document doc = searcher.doc(results.scoreDocs[i].doc);
			String path = doc.get("path");
			System.out.println((i + 1) + ". " + path);
			String title = doc.get("title");
			if (title != null) {
				System.out.println("   Title: " + doc.get("title"));
			}
		}
		reader.close();
	}
}