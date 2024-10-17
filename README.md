# COMP-8380 Information Retrieval Systems Project

This project depends on Maven

## Api Documentation

[Click Here]("./docs/webserver_api/webserver_api.md")

## Setup

1. This build of Lucene is meant to be run the OpenJDK 20. Use `mvn clean install`
2. Lucene will be installed automatically with Maven,
3. Download the [citeceer2 data](https://jlu.myweb.cs.uwindsor.ca/8380/citeseer2.tar.gz "citeceer2 Data") and fully extract it, placing it in the root of this project.
4. Indexer can be run using `mvn exec:java@run-indexer`
5. Searcher can be installed using `mvn exec:java@run-searcher`


## Explanation

### Indexer

Consider this code block

```java
	// Create the override analyzer for the content field to only match computer science terms.
	Map<String, Analyzer> overrides = new HashMap<>();
	overrides.put(Constants.FIELD_KEYWORDS, new KeyTermsAnalyzer());

	// Writer for our indexing.
	IndexWriter writer = new IndexWriter(
			// The root path for the directory to index.
			FSDirectory.open(Paths.get(Constants.DATA_INDEX)),
			// Set the default analyzer to match everything and override for the keywords.
			new IndexWriterConfig(new PerFieldAnalyzerWrapper(new StandardAnalyzer(), overrides)));
```

We map Fields to Analyzer, we are specifying how we will analyzer each field we are going to index.
In this snippet we make a field called keywords and we will use our `KeyTermsAnalyzer` class to run logic on the document to index this class. It is responisble for tokenizing the text into the keywords specified in `terms.txt`. 