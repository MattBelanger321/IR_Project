package ca.uwindsor.analyzing;

import java.io.*;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Path;
import java.util.TreeSet;

import ca.uwindsor.common.TermsCollection;
import org.apache.logging.log4j.LogManager;
import org.apache.logging.log4j.Logger;
import org.apache.lucene.analysis.Analyzer;
import org.apache.lucene.analysis.TokenStream;
import org.apache.lucene.analysis.Tokenizer;
import org.apache.lucene.analysis.core.LowerCaseFilter;
import org.apache.lucene.analysis.en.PorterStemFilter;
import org.apache.lucene.analysis.standard.StandardTokenizer;
import org.apache.lucene.analysis.synonym.SynonymGraphFilter;
import org.apache.lucene.analysis.synonym.SynonymMap;
import org.apache.lucene.analysis.tokenattributes.CharTermAttribute;

import ca.uwindsor.common.Constants;
import org.apache.lucene.util.CharsRef;

/**
 * Implement custom logic for tokenizing and stemming.
 */
public class ComputerScienceAnalyzer extends Analyzer
{
    /**
     * The logger used for this class.
     */
    private static final Logger logger = LogManager.getLogger(ComputerScienceAnalyzer.class);

    /**
     * If this should look for key terms only.
     */
    private final boolean keyTermsOnly;

    /**
     * Loaded stems.
     */
    private static TreeSet<String> stems = null;

    /**
     * Loaded keywords.
     */
    private static TreeSet<String> keywords = null;

    /**
     * Loaded abbreviations.
     */
    private static SynonymMap abbreviations = null;

    /**
     * Set up the custom analyzer.
     *
     * @param keyTermsOnly Whether this analyzer should only use key terms or not.
     */
    public ComputerScienceAnalyzer(Boolean keyTermsOnly)
    {
        super();

        // Store if we are only interested in key terms.
        this.keyTermsOnly = keyTermsOnly;

        // Nothing to do if the terms have already been loaded.
        if (stems != null && keywords != null && abbreviations != null)
        {
            return;
        }

        // Load the stems.
        stems = new TreeSet<>();
        TermsCollection.Load(stems, Constants.STEMS_FILE);

        // Load the keywords which will still need parsing.
        TreeSet<String> initialKeywords = new TreeSet<>();
        TermsCollection.Load(initialKeywords, Constants.KEY_TERMS);

        // Build the actual keywords and abbreviations mapping.
        keywords = new TreeSet<>();
        SynonymMap.Builder builder = new SynonymMap.Builder(true);
        for (String keyword : initialKeywords)
        {
            // Keywords split on the pipe symbol represent an abbreviation.
            String[] splits = keyword.split("\\|");
            splits[0] = splits[0].trim();

            // Check if something stems this keyword.
            try (Analyzer analyzer = new Analyzer()
            {
                @Override
                protected TokenStreamComponents createComponents(String fieldName)
                {
                    // The base is the standard tokenizer.
                    Tokenizer tokenizer = new StandardTokenizer();

                    // Perform our base tokenization.
                    return new TokenStreamComponents(tokenizer, baseTokenStream(tokenizer));
                }
            })
            {
                TokenStream stream = analyzer.tokenStream(null, splits[0]);
                StringBuilder sb = new StringBuilder();
                CharTermAttribute attr = stream.addAttribute(CharTermAttribute.class);
                stream.reset();

                // Stem each part of the term, joining it back together.
                while (stream.incrementToken())
                {
                    sb.append(attr.toString()).append(" ");
                }

                stream.end();
                stream.close();
                keyword = sb.toString().trim();
            } catch (IOException e)
            {
                keyword = splits[0];
                logger.error(e);
            }

            keywords.add(keyword);
            logger.debug("Keyword: " + keyword);

            // If there are maps, ensure they are converted to the keywords.
            for (int i = 1; i < splits.length; i++)
            {
                String abbreviation = splits[i].trim();
                builder.add(new CharsRef(abbreviation), new CharsRef(keyword), false);
                logger.debug("Keyword: " + keyword + " | Abbreviation: " + abbreviation);
            }
        }

        // Set up the abbreviations.
        try
        {
            abbreviations = builder.build();
        } catch (IOException e)
        {
            logger.error(e);
        }
    }

    /**
     * Determine how we should tokenize a field.
     *
     * @param fieldName The field to tokenize which we do not use.
     * @return The full tokenization process.
     */
    @Override
    protected TokenStreamComponents createComponents(String fieldName)
    {
        return fullTokenStream(keyTermsOnly);
    }

    /**
     * The full tokenization process.
     *
     * @param keyTermsOnly Whether this analyzer should only use key terms or not.
     * @return The full tokenization process.
     */
    private static TokenStreamComponents fullTokenStream(boolean keyTermsOnly)
    {
        // The base is the standard tokenizer.
        Tokenizer tokenizer = new StandardTokenizer();

        // Perform our base tokenization followed by changing any abbreviations into their keywords.
        TokenStream tokenStream = new SynonymGraphFilter(baseTokenStream(tokenizer), abbreviations, true);

        // If only interested in the keywords, filter it to just them.
        if (keyTermsOnly)
        {
            return new TokenStreamComponents(tokenizer, new KeyTermsFilter(tokenStream, keywords));
        }

        // Otherwise, return everything.
        return new TokenStreamComponents(tokenizer, tokenStream);
    }

    /**
     * The base part of the tokenization, both during initialization and the indexing and searching.
     *
     * @param tokenizer The tokenizer to use.
     * @return The core lowercase and stemming portion of the pipeline.
     */
    private static TokenStream baseTokenStream(Tokenizer tokenizer)
    {
        // Ensure we are in lowercase.
        TokenStream tokenStream = new LowerCaseFilter(tokenizer);

        // Perform our custom stemming followed by the porter stemmer on top of it.
        return new PorterStemFilter(new CustomStemFilter(tokenStream, stems));
    }

    /**
     * Analyze a file, writing it to files.
     *
     * @param file   The file to analyze.
     * @param folder The folder to save the result to.
     * @param output The root name of the output file.
     * @throws IOException An error reading the file.
     */
    public static void analyzeFile(Path file, String folder, String output) throws IOException
    {
        // Read the file.
        InputStream stream = Files.newInputStream(file);
        InputStreamReader inputStreamReader = new InputStreamReader(stream, StandardCharsets.UTF_8);
        BufferedReader reader = new BufferedReader(inputStreamReader);

        String line;
        StringBuilder contents = new StringBuilder();
        while ((line = reader.readLine()) != null)
        {
            contents.append(line).append(System.lineSeparator());
        }

        String raw = contents.toString();
        try (BufferedWriter writer = new BufferedWriter(new FileWriter(folder + "/" + output + " Raw.txt")))
        {
            writer.write(raw);
        }

        String stemmed = analyzeText(raw, false);
        try (BufferedWriter writer = new BufferedWriter(new FileWriter(folder + "/" + output + " Stemmed.txt")))
        {
            writer.write(stemmed);
        }

        String keyTerms = analyzeText(raw, true);
        try (BufferedWriter writer = new BufferedWriter(new FileWriter(folder + "/" + output + " Key Terms.txt")))
        {
            writer.write(keyTerms);
        }
    }

    /**
     * A function used for debugging that will return the transformed text.
     *
     * @param contents     The contents to analyze.
     * @param keyTermsOnly Whether this analyzer should only use key terms or not.
     * @return The analyzed text.
     */
    public static String analyzeText(String contents, boolean keyTermsOnly)
    {
        StringBuilder result = new StringBuilder();

        try (Analyzer analyzer = new Analyzer()
        {
            @Override
            protected TokenStreamComponents createComponents(String fieldName)
            {
                return fullTokenStream(keyTermsOnly);
            }
        })
        {
            TokenStream tokenStream = analyzer.tokenStream(null, contents);
            CharTermAttribute attr = tokenStream.addAttribute(CharTermAttribute.class);
            tokenStream.reset();

            // Stem each part of the term, joining it back together.
            while (tokenStream.incrementToken())
            {
                result.append(attr.toString()).append(" ");
            }

            tokenStream.end();
            tokenStream.close();
        } catch (IOException e)
        {
            logger.error(e);
        }

        return result.toString().trim();
    }
}