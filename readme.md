# Search Engine

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

# Running

## Developing

- Follow these steps if you want to build your [arXiv](https://arxiv.org "arXiv") dataset for the first time.
1. Ensure [Ollama](https://ollama.com "Ollama") is running.
2. Run ``Builder`` which will fail eventually if there is no ``embeddings.txt`` file which is what the next step is for.
3. Run ``word2vec.py``. **From the ``embeddings`` folder, copy over an embeddings file to the root of your application.** This will be the embeddings used at runtime. It is recommended you use the ``embeddings.txt`` file directly under ``embeddings`` rather than the category specific ones for the best results.
4. Launch [Qdrant](https://github.com/qdrant/qdrant "Qdrant") with [Docker](https://www.docker.com) with ``docker run -p 6334:6334 qdrant/qdrant``.
5. Run ``Builder`` again.

### Optional

- To generate plots, you can run the following two files with the following arguments.
1. ``plot_embedding.py [-h] --method {pca,tsne} [--output OUTPUT] [--category CATEGORY] [--perplexity PERPLEXITY]``
2. ``text_statistics.py [-h] directory output``

## Serving

1. Launch [Qdrant](https://github.com/qdrant/qdrant "Qdrant") with [Docker](https://www.docker.com) with ``docker run -p 6334:6334 qdrant/qdrant``.
2. Run ``Server: IIS Express`` or ``Server``.

# Project Structure

## Builder - C#

This will handle one-time building and indexing of the dataset from [arXiv](https://arxiv.org "arXiv"), summarizing it with [Ollama](https://ollama.com "Ollama"), and indexing it into [Qdrant](https://github.com/qdrant/qdrant "Qdrant").

## Server - C#

- Hosts the backend server so we can run searches from our client.
- This also contains all [Qdrant](https://github.com/qdrant/qdrant "Qdrant") methods for indexing and searching.
- Will automatically scrape and index more [arXiv](https://arxiv.org "arXiv") data.

## Client - C#

- The client project providing the UI for searching.

## Shared - C#

- Holds the common data structure for passing data between the other projects.

## word2vec - Python

- Generates vector embeddings with word2vec.

# [arXiv](https://arxiv.org "arXiv") Data Format

- The scrapped [arXiv](https://arxiv.org "arXiv") data is organized into subfolders based on the computer science category they are from.
  - [All categories are can be found here.](https://arxiv.org/archive/cs "arXiv Computer Science Categories")
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
- The sixth link has the IDs of all [arXiv](https://arxiv.org "arXiv") documents which this document links to.
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