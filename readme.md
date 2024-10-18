# Search Engine

- Allows for querying papers from [arXiv](https://arxiv.org "arXiv").
- Written in [C# .NET](https://dotnet.microsoft.com ".NET").
- Uses [Lucene.NET](https://lucenenet.apache.org "Lucene.NET") for indexing.

# Getting Started

1. You need to have the required [.NET](https://dotnet.microsoft.com ".NET") tools intalled.
    1. The easiest way to do this (even if you don't end up using it) is to install [Visual Studio](https://visualstudio.microsoft.com "Visual Studio"). When doing the install of [Visual Studio](https://visualstudio.microsoft.com "Visual Studio"), simply check the "ASP.NET and web development" option and it will install everything you need. You could alternatively manually [download .NET](https://dotnet.microsoft.com/en-us/download ".NET Download") and configure it.
   2. In an IDE of your choice ([Rider](https://www.jetbrains.com/rider "Rider"), [Visual Studio](https://visualstudio.microsoft.com "Visual Studio"), [Visual Studio Code](https://code.visualstudio.com "VS Code")), open the solution file "SearchEngine.sln".
      - **[Rider](https://www.jetbrains.com/rider "Rider"), and all other [JetBrains products](https://www.jetbrains.com), are free for students!** I have worked with C# and [.NET](https://dotnet.microsoft.com ".NET") for years professionally, and I highly recommend using [Rider](https://www.jetbrains.com/rider "Rider") if you have the option. [You can get it for free here.](https://www.jetbrains.com/shop/eform/students "JetBrains Students")
   3. There are multiple running processes configured in the file, but for the most part, you will want to only use two of them.
      - ``Indexer`` - Runs downloading of files from [arXiv](https://arxiv.org "arXiv") and indexes them using [Lucene.NET](https://lucenenet.apache.org "Lucene.NET").
      - ``Server: IIS Express`` - **After indexing, use this during your development as it will be the easiest!** This will start the server, but also the client along with debugging tools.
      - ``Client: IIS Express`` - Pointless to run as the server will not be up with it.
      - ``Server`` and ``Client`` - If you ever look to deploy this for real, you would likely look to automate these in CI/CD pipelines, but for development, using ``Server: IIS Express`` is likely easier.

# Project Structure

- This solution consists of five subprojects which work together.

## Lucene

- This handles all [Lucene.NET](https://lucenenet.apache.org "Lucene.NET") operations, ensuring that Lucene dependencies are not needed in the other projects.

## Indexer

- This will run the downloading from [arXiv](https://arxiv.org "arXiv") and then indexing with [Lucene.NET](https://lucenenet.apache.org "Lucene.NET")

## Shared

- Holds the common data structure for passing data between the other projects.

## Server

- Hosts the backend server so we can run searches from our client.

## Client

- The client project providing the UI for searching.