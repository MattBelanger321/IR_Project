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
import java.util.Map;

import ca.uwindsor.analyzing.ComputerScienceAnalyzer;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.miscellaneous.PerFieldAnalyzerWrapper;
import org.apache.lucene.document.Document;
import org.apache.lucene.document.Field;
import org.apache.lucene.document.FieldType;
import org.apache.lucene.document.StringField;
import org.apache.lucene.document.TextField;
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
     * The logger used for this class.
     */
    private static final Logger logger = LogManager.getLogger(Indexer.class);

    /**
     * Count how many files have been indexed.
     */
    private static int counter = 0;

    /**
     * Custom field used for the keywords only section.
     */
    private static FieldType storeAll;

    /**
     * Run the indexing.
     *
     * @param args Nothing.
     * @throws IOException If a file reading error occurs during execution.
     */
    public static void main(String[] args) throws IOException
    {
        // Define our custom field to store the frequency of terms.
        storeAll = new FieldType(TextField.TYPE_STORED);
        storeAll.setStoreTermVectors(true);
        storeAll.setStoreTermVectorPositions(true);
        storeAll.setStoreTermVectorOffsets(true);
        storeAll.setTokenized(true);
        storeAll.setIndexOptions(IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS);

        // Create the override analyzer for the content field to only match computer science terms.
        Map<String, Analyzer> overrides = new HashMap<>();
        overrides.put(Constants.FieldNames.KEYWORDS.getValue(), new ComputerScienceAnalyzer(true));

        // Writer for our indexing.
        IndexWriter writer = new IndexWriter(
                // The root path for the directory to index.
                FSDirectory.open(Paths.get(Constants.DATA_INDEX)),
                // Set the default analyzer to match words, and our override for keywords.
                new IndexWriterConfig(new PerFieldAnalyzerWrapper(new ComputerScienceAnalyzer(false), overrides)));

        // Loop over all files to index.
        Files.walkFileTree(Paths.get(Constants.DATA), new SimpleFileVisitor<Path>()
        {
            // Visit the file and index it.
            @Override
            public FileVisitResult visitFile(Path file, BasicFileAttributes attrs)
            {
                try
                {
                    indexDoc(writer, file);
                } catch (Exception e)
                {
                    logger.error(e);
                }

                if (++counter % 1000 == 0)
                {
                    logger.info("Indexed file #" + counter + ": " + file);
                }

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
        doc.add(new StringField(Constants.FieldNames.PATH.getValue(), file.toString(), Field.Store.YES));
        doc.add(new StringField(Constants.FieldNames.TITLE.getValue(), title, Field.Store.YES));

        if (contents.length() > 0)
        {
            // The contents are tokenized normally.
            doc.add(new TextField(Constants.FieldNames.CONTENTS.getValue(), contents.toString(), Field.Store.NO));

            // The keywords are stored noting their frequency.
            doc.add(new Field(Constants.FieldNames.KEYWORDS.getValue(), contents.toString(), storeAll));

            // The contents are tokenized using the custom stemming.
            doc.add(new Field(Constants.FieldNames.STEMMED_CONTENTS.getValue(), contents.toString(), storeAll));
        }

        // Index the document.
        writer.addDocument(doc);

        // Cleanup the readers.
        reader.close();
        inputStreamReader.close();
        stream.close();
    }
}