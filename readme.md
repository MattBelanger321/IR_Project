# Search Engine

- [Overview](#overview)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
  - [C#](#c)
    - [Builder](#builder)
    - [Server](#server)
  - [Python](#python)
    - [model_fitting](#model_fitting)
    - [plot_embeddings](#plot_embeddings)
    - [text_statistics](#text_statistics)
    - [training_set](#training_set)
- [Running](#running)
  - [Developing](#developing)
  - [Serving](#serving)
- [arXiv Data-Format](#arxiv-data-format)
- [Key Terms and Abbreviations](#key-terms-and-abbreviations)
- [Stems](#stems)

# Overview

- Written in [C# .NET](https://dotnet.microsoft.com ".NET") with vector embeddings generated using [gensim](https://pypi.org/project/gensim) in Python.
- Download entries from [arXiv](https://arxiv.org "arXiv") to be indexed by [Qdrant](https://github.com/qdrant/qdrant "Qdrant").
- The indexing and searching has lemmatization of key terms, stemming using both a custom and Porter stemmer, and stop words removal.
    - Custom key terms and stems can be added via text files.
- Search by either query or find similar papers to a specific one.
- Infinite scroll to load more results.
- Uses [Ollama](https://ollama.com "Ollama") to produce concise summaries with a local large language model.
- Uses [Hunspell](https://hunspell.github.io) for spell correction.

# Getting Started

1. This will require installing Python.
2. It is recommended you create a virtual environment for Python.
3. Install the requirements.txt file for the Python packages.
4. Install [Docker](https://www.docker.com).
5. Install [Ollama](https://ollama.com "Ollama"), and ensure it is running.
6. You need to have the required [.NET](https://dotnet.microsoft.com ".NET") tools installed.
   1. The easiest way to do this (even if you don't end up using it) is to install [Visual Studio](https://visualstudio.microsoft.com "Visual Studio"). When doing the installation of [Visual Studio](https://visualstudio.microsoft.com "Visual Studio"), simply check the "ASP.NET and web development" option, and it will install everything you need.
   2. You could alternatively manually [download .NET](https://dotnet.microsoft.com/en-us/download ".NET Download") and configure it.
7. In an IDE of your choice ([Rider](https://www.jetbrains.com/rider "Rider"), [Visual Studio](https://visualstudio.microsoft.com "Visual Studio"), [Visual Studio Code](https://code.visualstudio.com "VS Code")), open the solution file "SearchEngine.sln".
   - **[Rider](https://www.jetbrains.com/rider "Rider"), and all other [JetBrains products](https://www.jetbrains.com), are free for students!** I have worked with C# and [.NET](https://dotnet.microsoft.com ".NET") for years professionally, and I highly recommend using [Rider](https://www.jetbrains.com/rider "Rider") if you have the option.
   - [You can get it for free here.](https://www.jetbrains.com/shop/eform/students "JetBrains Students")

# Project Structure

All subprojects and scripts which can be directly run are listed here. If something is not listed here, it is either a helper subproject or file which is not directly run itself.

## C#

### Builder

- This will handle one-time building and indexing of the dataset from [arXiv](https://arxiv.org "arXiv"), summarizing it with [Ollama](https://ollama.com "Ollama"), and indexing it into [Qdrant](https://github.com/qdrant/qdrant "Qdrant").
- Arguments are positional and do not require a prefix like the Python scripts in this repository do.
- Default values will apply if you do not pass anything.

1. ``Total Results`` - Integer - The amount of [arXiv](https://arxiv.org "arXiv") data to download. Defaults to ``100000``. **Note you will likely not be able to download more than 1,500,000 in total due to [arXiv](https://arxiv.org "arXiv") API limitations.**
2. ``Lower Clustering Bound`` - Integer - The lower number of clusters to fit to. Defaults to ``5``.
3. ``Upper Clustering Bound`` - Integer - The upper number of clusters to fit to. Defaults to ``5``.
4. ``Run Mitigation`` - Boolean - If mitigation should be run. Defaults to ``true``.
5. ``Run Clustering`` - Boolean - If clustering should be run. Defaults to ``true``.
6. ``Run PageRank`` - Boolean - If PageRank should be run. Defaults to ``true``.
7. ``Run Summarzing`` - Boolean - If summarizing with [Ollama](https://ollama.com "Ollama") should be run. Defaults to ``true``.
8. ``Run Indexing`` - Boolean - If results should be indexed into the [Qdrant](https://github.com/qdrant/qdrant "Qdrant") database. Defaults to ``true``.
9. ``Reset Indexing`` - Boolean - If the [Qdrant](https://github.com/qdrant/qdrant "Qdrant") database should be reset before indexing. Defaults to ``true``.
10. ``Starting Category`` - String or Null - What [arXiv](https://arxiv.org "arXiv") category to start searching from. Defaults to ``null`` to run from the beginning.
11. ``Starting Order`` - String or Null - What [arXiv](https://arxiv.org "arXiv") order to start searching from. Defaults to ``null`` to run from the beginning.
12. ``Starting Sort By Mode`` - String or Null - What [arXiv](https://arxiv.org "arXiv") sort by mode to start searching from. Defaults to ``null`` to run from the beginning.

### Server

- Hosts the backend server so we can run searches from our client.
- This also contains all [Qdrant](https://github.com/qdrant/qdrant "Qdrant") methods for indexing and searching.
- Will automatically scrape and index more [arXiv](https://arxiv.org "arXiv") data.
- This takes a single boolean argument to run the scarping service automatically in the background on repeat. Defaults to ``false``.

## Python

### model_fitting

- Generates word2vec embeddings.
- Checks classification on word2vec models and two language models.
- ``-d`` ``--directory`` - The folder to load data from. Defaults to ``arXiv_processed_mitigated``.
- ``-s`` ``--seed`` - The seed for random state. Defaults to ``42``.
- ``-al`` ``--alpha_low`` - The lower bound for word2vec alpha values. Defaults to ``0.01``.
- ``-au`` ``--alpha_upper`` - The upper bound for word2vec alpha values. Defaults to ``0.05``.
- ``-as`` ``--alpha_step`` - The step for word2vec alpha values. Defaults to ``0.01``.
- ``-wl`` ``--window_low`` - The lower bound for word2vec window values. Defaults to ``5``.
- ``-wu`` ``--window_upper`` - The upper bound for word2vec window values. Defaults to ``10``.
- ``-ws`` ``--window_step`` - The step for word2vec window values. Defaults to ``5``.
- ``-nl`` ``--negative_low`` - The lower bound for word2vec negative values. Defaults to ``5``.
- ``-nu`` ``--negative_upper`` - The upper bound for word2vec negative values. Defaults to ``10``.
- ``-ns`` ``--negative_step`` - The step for word2vec negative values. Defaults to ``5``.

### plot_embeddings

- Plot out PCA and t-SNE for an embeddings file.
- ``-e`` ``--embeddings`` - The embeddings file to use. Defaults to ``embeddings.txt``.
- ``-o`` ``--output`` - The folder to output plots to. Defaults to ``Plots``.
- ``-p`` ``--perplexity`` - Perplexity for t-SNE. Defaults to ``20``.
- ``-s`` ``--size`` - The size of the plots. Defaults to ``100``.
- ``-n`` ``--no-labels`` - Disable labels.

### text_statistics

- Calculate Heaps' Law, Zipf's Law, and get the most frequent terms in the corpus.
- ``-d`` ``--directory`` - The root folder to run these statistics on. Defaults to ``arXiv_processed``.
- ``-o`` ``--output`` - The output directory to save to. Defaults to ``Text Statistics``.
- ``-x`` ``--width`` - The output width. Defaults to ``10``.
- ``-y`` ``--height`` - The output height. Defaults to ``5``.

### training_set

- Allows you to make a smaller training set of data if your corpus is very large.
- ``-d`` ``--directory`` - The root folder to build a training set for. Defaults to ``arXiv``.
- ``-s`` ``--size`` - The amount to use for a training set, either as a percentage as a float in the range (0, 1] or the number of files to copy as an integer. Defaults to ``0.1``.
- ``-r`` ``--seed`` - The seed for randomly choosing files. Defaults to ``42``.

# Running

## Developing

1. Launch [Ollama](https://ollama.com "Ollama") is running.
2. Run ``Builder`` with ``NUMBER MIN MAX true false false true false``. See above for what these commands are.
3. Run ``model_fitting``.
4. From the ``Embeddings`` folder under ``arXiv_processed_mitigated``, copy over an embeddings file to the root of your application and name it ``embeddings.txt`.
5. Launch [Qdrant](https://github.com/qdrant/qdrant "Qdrant") with [Docker](https://www.docker.com) with ``docker run -p 6334:6334 qdrant/qdrant``.
6. Run ``Builder`` again with ``NUMBER MIN MAX false true true false true``.

## Serving

1. Launch [Qdrant](https://github.com/qdrant/qdrant "Qdrant") with [Docker](https://www.docker.com) with ``docker run -p 6334:6334 qdrant/qdrant``.
2. Run ``Server``.

# [arXiv](https://arxiv.org "arXiv") Data Format

- The scrapped [arXiv](https://arxiv.org "arXiv") data is organized into subfolders based on their primary category.
- The name of the file is the ID on [arXiv](https://arxiv.org "arXiv") as a text (.txt) file.
   - This can be used to get the main (abstract) page of the document, the PDF, or for newer papers, their experimental HTML pages of the papers as seen below where you replace "ID" with the file name (less the ".txt").
   - Main/abstract page - https://arxiv.org/abs/ID
  - PDF - https://arxiv.org/pdf/ID
  - HTML (Note that not all documents may have this) - https://arxiv.org/html/ID
- The first line of a text file contains the title of the document.
- The second line contains the abstract.
- The third line has the date and time in the format of "YYYY-DD-MM hh:mm:ss".
- The fourth line has all authors separated by a "|".
- The fifth line has all categories separated by a "|", with the primary category being the first one.
- All text has been preprocessed, ensuring all whitespace has been replaced by single spaces. Additionally, LaTeX/Markdown has been converted over to plain text.

# Key Terms and Abbreviations

- Our pipeline automatically replaces abbreviations with their term. For instance, "LLM" automatically becomes "Large Language Model". This ensures the indexing process treats a term and their abbreviation equally.
- Key terms can be found in ``terms.txt``. These have the following format: ``term|abbreviation1|abbreviation2|...|abbreviationN``. You can have as many abbreviations as you want for a term. Everything is normalized to lowercase in our pipeline.
- Our key terms builder automatically handles plurals. For instance, you only need to have ``large language model|llm`` and not ``large language model|llms`` or ``large language models|llms``.
- The key terms also recognize and remove the instances where abbreviations are introduced. For instance, in a paper it is common to for instance write "Large Language Models (LLMs)" the first time large language models are introduced. Our pipeline will reduce all such instances of this to just the term. This is done so the indexing process only recognize this as the term being written once, and not twice as the second was just introducing the abbreviation to the reader. We make sure to capture all possible plural combinations. Examples of this are below:
  - "Large Language Model (LLM)" becomes "Large Language Model".
  - "Large Language Model (LLMs)" becomes "Large Language Model".
  - "Large Language Models (LLM)" becomes "Large Language Models".
  - "Large Language Models (LLMs)" becomes "Large Language Models".

# Stems

- Custom stems can be found in ``stems.txt``.