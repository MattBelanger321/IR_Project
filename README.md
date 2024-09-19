# COMP-8380 Information Retrieval Systems Project

This project depends on Maven

## Setup

1. This build of Lucene is meant to be run the OpenJDK 20. Use `mvn clean install`
2. Lucene will be installed automatically with Maven,
3. Download the [citeceer2 data](https://jlu.myweb.cs.uwindsor.ca/8380/citeseer2.tar.gz "citeceer2 Data") and fully extract it, placing it in the root of this project.
4. Indexer can be run using `mvn exec:java -Dexec.mainClass="ca.uwindsor.IndexAllFilesInDirectory"`
5. Searcher can be installed using `mvn exec:java -Dexec.mainClass="ca.uwindsor.SearchIndexedDocs"`